using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter
{

    [TestClass]
    public class GoogleTestDiscovererTests : AbstractGoogleTestExtensionTests
    {
        public const string x86staticallyLinkedTests = @"..\..\..\testdata\_x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x86externallyLinkedTests = @"..\..\..\testdata\_x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x86crashingTests = @"..\..\..\testdata\_x86\CrashingGoogleTests\CrashingGoogleTests.exe";
        public const string x64staticallyLinkedTests = @"..\..\..\testdata\_x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        public const string x64externallyLinkedTests = @"..\..\..\testdata\_x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        public const string x64crashingTests = @"..\..\..\testdata\_x64\CrashingGoogleTests\CrashingGoogleTests.exe";

        public const string x86traitsTests = @"..\..\..\..\ConsoleApplication1\Debug\ConsoleApplication1Tests.exe";

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
            Mock<IDiscoveryContext> MockDiscoveryContext = new Mock<IDiscoveryContext>();
            Mock<ITestCaseDiscoverySink> MockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            MockOptions.Setup(O => O.TestDiscoveryRegex).Returns(() => customRegex);

            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            Discoverer.DiscoverTests(x86staticallyLinkedTests.Yield(), MockDiscoveryContext.Object, MockLogger.Object, MockDiscoverySink.Object);

            MockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<TestCase>()), Times.Exactly(expectedNrOfTests));
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            var Tests = Discoverer.GetTestsFromExecutable(MockLogger.Object, location);
            Assert.AreEqual(2, Tests.Count);
            Assert.AreEqual("FooTest.DoesXyz", Tests[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", Tests[0].CodeFilePath);
            Assert.AreEqual(45, Tests[0].LineNumber);
            Assert.AreEqual("FooTest.MethodBarDoesAbc", Tests[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", Tests[1].CodeFilePath);
            Assert.AreEqual(36, Tests[1].LineNumber);
        }

        [TestMethod]
        public void FindsTestsFromStaticallyLinkedX86ExecutableWithSourceFileLocation()
        {
            FindStaticallyLinkedTests(x86staticallyLinkedTests);
        }

        [TestMethod]
        public void FindsTestsFromStaticallyLinkedX64ExecutableWithSourceFileLocation()
        {
            FindStaticallyLinkedTests(x64staticallyLinkedTests);
        }


        private void FindExternallyLinkedTests(string location)
        {
            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            var Tests = Discoverer.GetTestsFromExecutable(MockLogger.Object, location);

            Assert.AreEqual(2, Tests.Count);

            Assert.AreEqual("BarTest.DoesXyz", Tests[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", Tests[0].CodeFilePath);
            Assert.AreEqual(44, Tests[0].LineNumber);

            Assert.AreEqual("BarTest.MethodBarDoesAbc", Tests[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", Tests[1].CodeFilePath);
            Assert.AreEqual(36, Tests[1].LineNumber);
        }

        [TestMethod]
        public void FindsTestsFromExternallyLinkedX86ExecutableWithSourceFileLocation()
        {
            FindExternallyLinkedTests(x86externallyLinkedTests);
        }

        [TestMethod]
        public void FindsTestsFromExternallyLinkedX64ExecutableWithSourceFileLocation()
        {
            FindExternallyLinkedTests(x64externallyLinkedTests);
        }

        [TestMethod]
        public void FindsMathTestWithOneTrait()
        {
            Trait[] Traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", Traits);
        }

        [TestMethod]
        public void FindsMathTestWithTwoTraits()
        {
            Trait[] Traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits2", Traits);
        }

        [TestMethod]
        public void FindsMathTestWithThreeTraits()
        {
            Trait[] Traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits3", Traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithOneTrait()
        {
            Trait[] Traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TheFixture.AddPassesWithTraits", Traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithTwoTraits()
        {
            Trait[] Traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("TheFixture.AddPassesWithTraits2", Traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithThreeTraits()
        {
            Trait[] Traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            FindsTestWithTraits("TheFixture.AddPassesWithTraits3", Traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithOneTrait()
        {
            Trait[] Traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits/0  # GetParam() = (1,)", Traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithTwoTraits()
        {
            Trait[] Traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits2/0  # GetParam() = (1,)", Traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithThreeTraits()
        {
            Trait[] Traits = { new Trait("Type", "Medium"), new Trait("Author", "MSI"), new Trait("Category", "Integration") };
            FindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits3/0  # GetParam() = (1,)", Traits);
        }

        [TestMethod]
        public void CustomTraitBeforeAddsTraitIfNotAlreadyExisting()
        {
            Trait[] Traits = { };
            FindsTestWithTraits("TestMath.AddPasses", Traits);

            MockOptions.Setup(O => O.TraitsRegexesBefore).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "SomeNewType").Yield().ToList());

            Traits = new[] { new Trait("Type", "SomeNewType") };
            FindsTestWithTraits("TestMath.AddPasses", Traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByTraitOfTest()
        {
            MockOptions.Setup(O => O.TraitsRegexesBefore).Returns(new RegexTraitPair("TestMath.AddPassesWithTraits", "Type", "SomeNewType").Yield().ToList());

            Trait[] Traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", Traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByCustomTraitAfter()
        {
            MockOptions.Setup(O => O.TraitsRegexesBefore).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "BeforeType").Yield().ToList());
            MockOptions.Setup(O => O.TraitsRegexesAfter).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "AfterType").Yield().ToList());

            Trait[] Traits = { new Trait("Type", "AfterType") };
            FindsTestWithTraits("TestMath.AddPasses", Traits);
        }

        [TestMethod]
        public void CustomTraitAfterOverridesTraitOfTest()
        {
            Trait[] Traits = { new Trait("Type", "Small") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", Traits);

            MockOptions.Setup(O => O.TraitsRegexesAfter).Returns(new RegexTraitPair("TestMath.AddPassesWithTraits", "Type", "SomeNewType").Yield().ToList());

            Traits = new[] { new Trait("Type", "SomeNewType") };
            FindsTestWithTraits("TestMath.AddPassesWithTraits", Traits);
        }

        [TestMethod]
        public void CustomTraitAfterAddsTraitIfNotAlreadyExisting()
        {
            Trait[] Traits = { };
            FindsTestWithTraits("TestMath.AddPasses", Traits);

            MockOptions.Setup(O => O.TraitsRegexesAfter).Returns(new RegexTraitPair("TestMath.AddPasses", "Type", "SomeNewType").Yield().ToList());

            Traits = new[] { new Trait("Type", "SomeNewType") };
            FindsTestWithTraits("TestMath.AddPasses", Traits);
        }

        private void FindsTestWithTraits(string fullyQualifiedName, Trait[] traits)
        {
            Assert.IsTrue(File.Exists(x86traitsTests), "Build ConsoleApplication1 in Debug mode before executing this test");

            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            List<TestCase> Tests = Discoverer.GetTestsFromExecutable(MockLogger.Object, x86traitsTests);

            TestCase TestCase = Tests.Find(tc => tc.Traits.Count() == traits.Length && tc.FullyQualifiedName == fullyQualifiedName);
            Assert.IsNotNull(TestCase);

            foreach (Trait Trait in traits)
            {
                Trait FoundTrait = TestCase.Traits.FirstOrDefault(T => Trait.Name == T.Name && Trait.Value == T.Value);
                Assert.IsNotNull(FoundTrait, "Didn't find trait: (" + Trait.Name + ", " + Trait.Value + ")");
            }
        }

    }

}