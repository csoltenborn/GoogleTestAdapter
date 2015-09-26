using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{

    [TestClass]
    public class GoogleTestDiscovererTests : AbstractGoogleTestExtensionTests
    {
        [TestMethod]
        public void MatchesTestExecutableName()
        {
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTest.exe", true);
            AssertIsGoogleTestExecutable("mygoogletests.exe", true);
            AssertIsGoogleTestExecutable("mygoogletest.exe", true);

            AssertIsGoogleTestExecutable("MyGoogleTes.exe", false);
            AssertIsGoogleTestExecutable("TotallyWrong.exe", false);
            AssertIsGoogleTestExecutable("TestStuff.exe", false);
            AssertIsGoogleTestExecutable("TestLibrary.exe", false);
        }

        [TestMethod]
        public void MatchesCustomRegex()
        {
            AssertIsGoogleTestExecutable("SomeWeirdExpression", true, "Some.*Expression");
            AssertIsGoogleTestExecutable("SomeWeirdOtherThing", false, "Some.*Expression");
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", false, "Some.*Expression");
        }

        private void AssertIsGoogleTestExecutable(string executable, bool isGoogleTestExecutable, string regex = "")
        {
            Assert.AreEqual(isGoogleTestExecutable,
                new GoogleTestDiscoverer(new TestEnvironment(MockOptions.Object, MockLogger.Object)).IsGoogleTestExecutable(executable, regex));
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

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(new TestEnvironment(MockOptions.Object, MockLogger.Object));
            discoverer.DiscoverTests(X86StaticallyLinkedTests.Yield(), mockDiscoveryContext.Object, MockLogger.Object, mockDiscoverySink.Object);

            mockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<TestCase>()), Times.Exactly(expectedNrOfTests));
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

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(new TestEnvironment(MockOptions.Object, MockLogger.Object));
            List<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(2, testCases.Count);

            Assert.AreEqual("FooTest.DoesXyz", testCases[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", testCases[0].CodeFilePath);
            Assert.AreEqual(45, testCases[0].LineNumber);

            Assert.AreEqual("FooTest.MethodBarDoesAbc", testCases[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", testCases[1].CodeFilePath);
            Assert.AreEqual(36, testCases[1].LineNumber);
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

        private void FindExternallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(new TestEnvironment(MockOptions.Object, MockLogger.Object));
            List<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(2, testCases.Count);

            Assert.AreEqual("BarTest.DoesXyz", testCases[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", testCases[0].CodeFilePath);
            Assert.AreEqual(44, testCases[0].LineNumber);

            Assert.AreEqual("BarTest.MethodBarDoesAbc", testCases[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", testCases[1].CodeFilePath);
            Assert.AreEqual(36, testCases[1].LineNumber);
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
            bool result = new GoogleTestDiscoverer(new TestEnvironment(MockOptions.Object, MockLogger.Object)).IsGoogleTestExecutable("my.exe", "d[ddd[");

            Assert.IsFalse(result);
            MockLogger.Verify(h => h.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("'d[ddd['"))),
                Times.Exactly(1));
        }

        private void FindsTestWithTraits(string fullyQualifiedName, Trait[] traits)
        {
            Assert.IsTrue(File.Exists(X86TraitsTests), "Build ConsoleApplication1 in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(new TestEnvironment(MockOptions.Object, MockLogger.Object));
            List<TestCase> tests = discoverer.GetTestsFromExecutable(X86TraitsTests);

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