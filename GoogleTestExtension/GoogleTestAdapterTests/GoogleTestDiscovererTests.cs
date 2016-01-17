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
        public void IsGoogleTestExecutable_MatchingExamples_AreMatched()
        {
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", true);
            AssertIsGoogleTestExecutable("MyGoogleTest.exe", true);
            AssertIsGoogleTestExecutable("mygoogletests.exe", true);
            AssertIsGoogleTestExecutable("mygoogletest.exe", true);
        }

        [TestMethod]
        public void IsGoogleTestExecutable_NotMatchingExamples_AreNotMatched()
        {
            AssertIsGoogleTestExecutable("MyGoogleTes.exe", false);
            AssertIsGoogleTestExecutable("TotallyWrong.exe", false);
            AssertIsGoogleTestExecutable("TestStuff.exe", false);
            AssertIsGoogleTestExecutable("TestLibrary.exe", false);
        }

        [TestMethod]
        public void IsGoogleTestExecutable_WithRegexFromOptions_MatchesCorrectly()
        {
            AssertIsGoogleTestExecutable("SomeWeirdExpression", true, "Some.*Expression");
            AssertIsGoogleTestExecutable("SomeWeirdOtherThing", false, "Some.*Expression");
            AssertIsGoogleTestExecutable("MyGoogleTests.exe", false, "Some.*Expression");
        }

        [TestMethod]
        public void IsGoogleTestExecutable_WithUnparsableRegexFromOptions_ProducesErrorMessage()
        {
            bool result = new GoogleTestDiscoverer(TestEnvironment).IsGoogleTestExecutable("my.exe", "d[ddd[");

            Assert.IsFalse(result);
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("'d[ddd['"))), Times.Exactly(1));
        }

        [TestMethod]
        public void GetTestsFromExecutable_StaticallyLinkedX86Executable_FindsTestsWitLocation()
        {
            FindStaticallyLinkedTests(X86StaticallyLinkedTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTestsWithLocation()
        {
            FindSampleTests(SampleTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_ExternallyLinkedX86Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(X86ExternallyLinkedTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_ExternallyLinkedX64Executable_FindsTestsWithLocation()
        {
            FindExternallyLinkedTests(X64ExternallyLinkedTests);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsMathTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsMathTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsMathTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Author", "JOG") };
            AssertFindsTestWithTraits("TypedTests/0.CanIterate", traits);
            AssertFindsTestWithTraits("TypedTests/1.CanIterate", traits);
            AssertFindsTestWithTraits("TypedTests/2.CanIterate", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Author", "IBM"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("TypedTests/0.TwoTraits", traits);
            AssertFindsTestWithTraits("TypedTests/1.TwoTraits", traits);
            AssertFindsTestWithTraits("TypedTests/2.TwoTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTestWithThreeTraits()
        {
            //ThreeTraits, Author, IBM, Category, Integration, Class, Simple
            Trait[] traits = { new Trait("Author", "IBM"), new Trait("Category", "Integration"), new Trait("Class", "Simple"), };
            AssertFindsTestWithTraits("TypedTests/0.ThreeTraits", traits);
            AssertFindsTestWithTraits("TypedTests/1.ThreeTraits", traits);
            AssertFindsTestWithTraits("TypedTests/2.ThreeTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("Vec/TypeParameterizedTests/0.CanIterate", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/0.CanIterate", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/1.CanIterate", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Author", "HAL"), new Trait("Category", "Unit") };
            AssertFindsTestWithTraits("Vec/TypeParameterizedTests/0.TwoTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/0.TwoTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/1.TwoTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithThreeTraits()
        {
            //ThreeTraits, Author, IBM, Category, Integration, Class, Simple
            Trait[] traits = { new Trait("Author", "HAL"), new Trait("Category", "Unit"), new Trait("Class", "Cake"), };
            AssertFindsTestWithTraits("Vec/TypeParameterizedTests/0.ThreeTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/0.ThreeTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/1.ThreeTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTests()
        {
            AssertFindsParameterizedTest(
                "InstantiationName/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]");

            AssertFindsParameterizedTest(
                "PointerParameterizedTests.CheckStringLength/2",
                new Regex("PointerParameterizedTests.CheckStringLength/2 ..[0-9A-F]+ pointing to .ooops., 23.."));
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsTypedTests()
        {
            AssertFindsParameterizedTest(
                "Arr/TypeParameterizedTests/0.CanIterate",
                "Arr/TypeParameterizedTests/0.CanIterate<std::array<int,3> >");

            AssertFindsParameterizedTest(
                "TypedTests/2.CanDefeatMath",
                "TypedTests/2.CanDefeatMath<MyStrangeArray>");

            AssertFindsParameterizedTest(
                "PrimitivelyTypedTests/0.CanHasBigNumbers",
                "PrimitivelyTypedTests/0.CanHasBigNumbers<signed char>");
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits2/0 [(1,)]", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Medium"), new Trait("Author", "MSI"), new Trait("Category", "Integration") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits3/0 [(1,)]", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_RegexBeforeFromOptions_AddsTraitIfNotAlreadyExisting()
        {
            string testname = "InstantiationName/ParameterizedTests.Simple/0 [(1,)]";
            Trait[] traits = { };
            AssertFindsTestWithTraits(testname, traits);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape(testname), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_RegexBeforeFromOptions_TraitFromOptionsIsOverridenByTraitFromTest()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_BothRegexesFromOptions_BeforeTraitIsOverridenByAfterTrait()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "BeforeType").Yield().ToList());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "AfterType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "AfterType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_RegexAfterFromOptions_AfterTraitOverridesTraitFromTest()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        public void GetTestsFromExecutable_RegexAfterFromOptions_AddsTraitIfNotAlreadyExisting()
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

        private void FindSampleTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

            Assert.AreEqual(61, testCases.Count);

            TestCase testCase =
               testCases.Single(tc => tc.FullyQualifiedName == "Arr/TypeParameterizedTests/1.CanDefeatMath");

            Assert.AreEqual("Arr/TypeParameterizedTests/1.CanDefeatMath<MyStrangeArray>", testCase.DisplayName);
            Assert.IsTrue(testCase.CodeFilePath.EndsWith(@"sampletests\tests\typeparameterizedtests.cpp"));
            Assert.AreEqual(53, testCase.LineNumber);
        }

        private void FindStaticallyLinkedTests(string location)
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

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
            IList<TestCase> testCases = discoverer.GetTestsFromExecutable(location);

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
            Assert.IsTrue(File.Exists(SampleTests), "Build SampleTests in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<TestCase> tests = discoverer.GetTestsFromExecutable(SampleTests).ToList();

            TestCase testCase = tests.Find(tc => tc.Traits.Count() == traits.Length && tc.DisplayName.StartsWith(displayName));
            Assert.IsNotNull(testCase);

            foreach (Trait trait in traits)
            {
                Trait foundTrait = testCase.Traits.FirstOrDefault(T => trait.Name == T.Name && trait.Value == T.Value);
                Assert.IsNotNull(foundTrait, "Didn't find trait: (" + trait.Name + ", " + trait.Value + ")");
            }
        }

        private void AssertFindsParameterizedTest(string fullyQualifiedName, string displayName)
        {
            AssertFindsParameterizedTest(fullyQualifiedName, new Regex(Regex.Escape(displayName)));
        }

        private void AssertFindsParameterizedTest(string fullyQualifiedName, Regex displayNameRegex)
        {
            Assert.IsTrue(File.Exists(SampleTests), "Build SampleTests in Debug mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            IList<TestCase> tests = discoverer.GetTestsFromExecutable(SampleTests);

            TestCase testCase = tests.Where(t => t.FullyQualifiedName == fullyQualifiedName).Single();
            Assert.IsTrue(displayNameRegex.IsMatch(testCase.DisplayName));
        }

    }

}