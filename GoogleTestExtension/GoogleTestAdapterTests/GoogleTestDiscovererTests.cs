using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
        public void UnparseableRegexProducesErrorMessage()
        {
            bool result = new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable("my.exe", "d[ddd[");

            Assert.IsFalse(result);
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("'d[ddd['"))), Times.Exactly(1));
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
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void FindsMathTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void FindsMathTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void FindsFixtureTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits2/0 [(1,)]", traits);
        }

        [TestMethod]
        public void FindsParameterizedTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Medium"), new Trait("Author", "MSI"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits3/0 [(1,)]", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeAddsTraitIfNotAlreadyExisting()
        {
            string testname = "InstantiationName/ParameterizedTests.Simple/0 [(1,)]";
            Trait[] traits = { };
            AssertFindsTestWithTraits(testname, traits);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape(testname), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByTraitOfTest()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void CustomTraitBeforeIsOverridenByCustomTraitAfter()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "BeforeType").Yield().ToList());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "AfterType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "AfterType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void CustomTraitAfterOverridesTraitOfTest()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void CustomTraitAfterAddsTraitIfNotAlreadyExisting()
        {
            Trait[] traits = { };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }


        private void AssertIsGoogleTestExecutable(string executable, bool isGoogleTestExecutable, string regex = "")
        {
            Assert.AreEqual(isGoogleTestExecutable,
                new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable(executable, regex));
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

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

        private void AssertFindsTestWithTraits(string displayName, Trait[] traits)
        {
            Assert.IsTrue(File.Exists(SampleTests), "Build ConsoleApplication1 in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<Model.TestCase> tests = discoverer.GetTestsFromExecutable(SampleTests);

            Model.TestCase testCase = tests.Find(tc => tc.Traits.Count() == traits.Length && tc.DisplayName == displayName);
            Assert.IsNotNull(testCase);

            foreach (Trait trait in traits)
            {
                Model.Trait foundTrait = testCase.Traits.FirstOrDefault(T => trait.Name == T.Name && trait.Value == T.Value);
                Assert.IsNotNull(foundTrait, "Didn't find trait: (" + trait.Name + ", " + trait.Value + ")");
            }
        }

    }

}