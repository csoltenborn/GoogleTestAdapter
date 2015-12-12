using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Moq;
using GoogleTestAdapter.Helpers;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Model;
using GoogleTestAdapterVSIX.TestFrameworkIntegration;

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
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void FindsMathTestWithTwoTraits()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void FindsMathTestWithThreeTraits()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Author", "CSO"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithOneTrait()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithTwoTraits()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithThreeTraits()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Author", "CSO"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithOneTrait()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithTwoTraits()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Author", "CSO") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits2/0 [(1,)]", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithThreeTraits()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Medium"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Author", "MSI"), new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Category", "Integration") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits3/0 [(1,)]", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeAddsTraitIfNotAlreadyExisting()
        {
            string testname = "InstantiationName/ParameterizedTests.Simple/0 [(1,)]";
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { };
            AssertFindsTestWithTraits(testname, traits);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape(testname), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByTraitOfTest()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByCustomTraitAfter()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "BeforeType").Yield().ToList());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "AfterType").Yield().ToList());

            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "AfterType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void CustomTraitAfterOverridesTraitOfTest()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void CustomTraitAfterAddsTraitIfNotAlreadyExisting()
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits = { };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void UnparseableRegexProducesErrorMessage()
        {
            bool result = new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable("my.exe", "d[ddd[");

            Assert.IsFalse(result);
            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("'d[ddd['"))),
                Times.Exactly(1));
        }


        private void AssertIsGoogleTestExecutable(string executable, bool isGoogleTestExecutable, string regex = "")
        {
            Assert.AreEqual(isGoogleTestExecutable,
                new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable(executable, regex));
        }

        private void CheckForDiscoverySinkCalls(int expectedNrOfTests, string customRegex = null)
        {
            Mock<IDiscoveryContext> mockDiscoveryContext = new Mock<IDiscoveryContext>();
            Mock<ITestCaseDiscoverySink> mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            MockOptions.Setup(o => o.TestDiscoveryRegex).Returns(() => customRegex);

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            discoverer.DiscoverTests(X86StaticallyLinkedTests.Yield(), mockDiscoveryContext.Object, MockLogger.Object, mockDiscoverySink.Object);

            mockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>()), Times.Exactly(expectedNrOfTests));
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<Model.TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(2, testCases.Count);

            Assert.AreEqual("FooTest.MethodBarDoesAbc", testCases[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", testCases[0].CodeFilePath);
            Assert.AreEqual(36, testCases[0].LineNumber);

            Assert.AreEqual("FooTest.DoesXyz", testCases[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp", testCases[1].CodeFilePath);
            Assert.AreEqual(45, testCases[1].LineNumber);
        }

        private void FindExternallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<Model.TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(2, testCases.Count);

            Assert.AreEqual("BarTest.MethodBarDoesAbc", testCases[0].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", testCases[0].CodeFilePath);
            Assert.AreEqual(36, testCases[0].LineNumber);

            Assert.AreEqual("BarTest.DoesXyz", testCases[1].DisplayName);
            Assert.AreEqual(@"c:\prod\gtest-1.7.0\externalgoogletestlibrary\externalgoogletestlibrarytests.cpp", testCases[1].CodeFilePath);
            Assert.AreEqual(44, testCases[1].LineNumber);
        }

        private void AssertFindsTestWithTraits(string displayName, Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait[] traits)
        {
            Assert.IsTrue(File.Exists(SampleTests), "Build ConsoleApplication1 in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<Model.TestCase> tests = discoverer.GetTestsFromExecutable(SampleTests);

            Model.TestCase testCase = tests.Find(tc => tc.Traits.Count() == traits.Length && tc.DisplayName == displayName);
            Assert.IsNotNull(testCase);

            foreach (Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait trait in traits)
            {
                Model.Trait foundTrait = testCase.Traits.FirstOrDefault(T => trait.Name == T.Name && trait.Value == T.Value);
                Assert.IsNotNull(foundTrait, "Didn't find trait: (" + trait.Name + ", " + trait.Value + ")");
            }
        }

    }

}