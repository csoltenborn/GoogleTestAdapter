using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{

    [TestClass]
    public class GoogleTestDiscovererTests : AbstractGoogleTestExtensionTests
    {
        public const string X86StaticallyLinkedTests = @"..\..\..\testdata\_x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string X86ExternallyLinkedTests = @"..\..\..\testdata\_x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string X86CrashingTests = @"..\..\..\testdata\_x86\CrashingGoogleTests\CrashingGoogleTests.exe";
        public const string X64StaticallyLinkedTests = @"..\..\..\testdata\_x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string X64ExternallyLinkedTests = @"..\..\..\testdata\_x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string X64CrashingTests = @"..\..\..\testdata\_x64\CrashingGoogleTests\CrashingGoogleTests.exe";

        public const string X86TraitsTests = @"..\..\..\..\ConsoleApplication1\Debug\ConsoleApplication1Tests.exe";
        public const string X86HardcrashingTests = @"..\..\..\..\ConsoleApplication1\Debug\ConsoleApplication1CrashingTests.exe";

        [TestMethod]
        public void MatchesTestExecutableName()
        {
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTests.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTest.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletests.exe", MockLogger.Object));
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("mygoogletest.exe", MockLogger.Object));

            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTes.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TotallyWrong.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TestStuff.exe", MockLogger.Object));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("TestLibrary.exe", MockLogger.Object));
        }

        [TestMethod]
        public void MatchesCustomRegex()
        {
            Assert.IsTrue(GoogleTestDiscoverer.IsGoogleTestExecutable("SomeWeirdExpression", MockLogger.Object, "Some.*Expression"));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("SomeWeirdOtherThing", MockLogger.Object, "Some.*Expression"));
            Assert.IsFalse(GoogleTestDiscoverer.IsGoogleTestExecutable("MyGoogleTests.exe", MockLogger.Object, "Some.*Expression"));
        }

        [TestMethod]
        public void RegistersFoundTestsAtDiscoverySink()
        {
            CheckForDiscoverySinkCalls(2);
        }

        [TestMethod]
        public void MatchesCustomRegexIfSetInOptions()
        {
            CheckForDiscoverySinkCalls(0, "NoMatchAtAll");
        }

        private void CheckForDiscoverySinkCalls(int expectedNrOfTests, string customRegex = null)
        {
            Mock<IDiscoveryContext> mockDiscoveryContext = new Mock<IDiscoveryContext>();
            Mock<ITestCaseDiscoverySink> mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            MockOptions.Setup(o => o.TestDiscoveryRegex).Returns(() => customRegex);

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            discoverer.DiscoverTests(X86StaticallyLinkedTests.Yield(), mockDiscoveryContext.Object, MockLogger.Object, mockDiscoverySink.Object);

            mockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<TestCase>()), Times.Exactly(expectedNrOfTests));
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            var tests = discoverer.GetTestsFromExecutable(MockLogger.Object, location);
            Assert.AreEqual(2, tests.Count);
            Assert.AreEqual("FooTest.DoesXyz", tests[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", tests[0].CodeFilePath);
            Assert.AreEqual(45, tests[0].LineNumber);
            Assert.AreEqual("FooTest.MethodBarDoesAbc", tests[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", tests[1].CodeFilePath);
            Assert.AreEqual(36, tests[1].LineNumber);
        }

        [TestMethod]
        public void FindsTestsFromStaticallyLinkedX86ExecutableWithSourceFileLocation()
        {
            FindStaticallyLinkedTests(X86StaticallyLinkedTests);
        }

        [TestMethod]
        public void FindsTestsFromStaticallyLinkedX64ExecutableWithSourceFileLocation()
        {
            FindStaticallyLinkedTests(X64StaticallyLinkedTests);
        }


        private void FindExternallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            var tests = discoverer.GetTestsFromExecutable(MockLogger.Object, location);

            Assert.AreEqual(2, tests.Count);

            Assert.AreEqual("BarTest.DoesXyz", tests[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", tests[0].CodeFilePath);
            Assert.AreEqual(44, tests[0].LineNumber);

            Assert.AreEqual("BarTest.MethodBarDoesAbc", tests[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", tests[1].CodeFilePath);
            Assert.AreEqual(36, tests[1].LineNumber);
        }

        [TestMethod]
        public void FindsTestsFromExternallyLinkedX86ExecutableWithSourceFileLocation()
        {
            FindExternallyLinkedTests(X86ExternallyLinkedTests);
        }

        [TestMethod]
        public void FindsTestsFromExternallyLinkedX64ExecutableWithSourceFileLocation()
        {
            FindExternallyLinkedTests(X64ExternallyLinkedTests);
        }

        [TestMethod]
        public void FindsMathTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void FindsMathTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void FindsMathTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TheFixture.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("TheFixture.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            FindsTestWithTraits("TheFixture.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits/0  # GetParam() = (1,)", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits2/0  # GetParam() = (1,)", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Medium"), new Trait("Author", "MSI"), new Trait("Category", "Integration") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits3/0  # GetParam() = (1,)", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeAddsTraitIfNotAlreadyExisting()
        {
            Trait[] traits = { };
            FindsTestWithTraits("TestMath.AddPasses", traits);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            FindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByTraitOfTest()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair("TestMath.AddPassesWithTraits", "Type", "SomeNewType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByCustomTraitAfter()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "BeforeType").Yield().ToList());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "AfterType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "AfterType") };
            FindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void CustomTraitAfterOverridesTraitOfTest()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair("TestMath.AddPassesWithTraits", "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void CustomTraitAfterAddsTraitIfNotAlreadyExisting()
        {
            Trait[] traits = { };
            FindsTestWithTraits("TestMath.AddPasses", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            FindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void UnparseableRegexProducesErrorMessage()
        {
            bool result = GoogleTestDiscoverer.IsGoogleTestExecutable("my.exe", MockLogger.Object, "d[ddd[");

            Assert.IsFalse(result);
            MockLogger.Verify(h => h.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("'d[ddd['"))), 
                Times.Exactly(1));
        }

        private void FindsTestWithTraits(string fullyQualifiedName, Trait[] traits)
        {
            Assert.IsTrue(File.Exists(X86TraitsTests), "Build ConsoleApplication1 in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            List<TestCase> tests = discoverer.GetTestsFromExecutable(MockLogger.Object, X86TraitsTests);

            TestCase testCase = tests.Find(tc => tc.Traits.Count() == traits.Length && tc.FullyQualifiedName == fullyQualifiedName);
            Assert.IsNotNull(testCase);

            foreach (Trait trait in traits)
            {
                Trait foundTrait = testCase.Traits.FirstOrDefault(T => trait.Name == T.Name && trait.Value == T.Value);
                Assert.IsNotNull(foundTrait, "Didn't find trait: (" + trait.Name + ", " + trait.Value + ")");
            }
        }

    }

}