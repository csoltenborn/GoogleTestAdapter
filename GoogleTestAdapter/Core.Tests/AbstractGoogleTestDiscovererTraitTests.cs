using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter
{
    [TestClass]
    public abstract class AbstractGoogleTestDiscovererTraitTests : AbstractCoreTests
    {
        protected abstract string SampleTestToUse { get; }


        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsMathTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Medium") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsMathTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsMathTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("TestCategory", "Integration") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits2", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO"), new Trait("TestCategory", "Integration") };
            AssertFindsTestWithTraits("TheFixture.AddPassesWithTraits3", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTypedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Author", "JOG") };
            AssertFindsTestWithTraits("TypedTests/0.CanIterate", traits);
            AssertFindsTestWithTraits("TypedTests/1.CanIterate", traits);
            AssertFindsTestWithTraits("TypedTests/2.CanIterate", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTypedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Author", "IBM"), new Trait("TestCategory", "Integration") };
            AssertFindsTestWithTraits("TypedTests/0.TwoTraits", traits);
            AssertFindsTestWithTraits("TypedTests/1.TwoTraits", traits);
            AssertFindsTestWithTraits("TypedTests/2.TwoTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTypedTestWithThreeTraits()
        {
            //ThreeTraits, Author, IBM, Category, Integration, Class, Simple
            Trait[] traits = { new Trait("Author", "IBM"), new Trait("TestCategory", "Integration"), new Trait("Class", "Simple"), };
            AssertFindsTestWithTraits("TypedTests/0.ThreeTraits", traits);
            AssertFindsTestWithTraits("TypedTests/1.ThreeTraits", traits);
            AssertFindsTestWithTraits("TypedTests/2.ThreeTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("Vec/TypeParameterizedTests/0.CanIterate", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/0.CanIterate", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/1.CanIterate", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Author", "HAL"), new Trait("TestCategory", Unit) };
            AssertFindsTestWithTraits("Vec/TypeParameterizedTests/0.TwoTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/0.TwoTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/1.TwoTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithThreeTraits()
        {
            //ThreeTraits, Author, IBM, Category, Integration, Class, Simple
            Trait[] traits = { new Trait("Author", "HAL"), new Trait("TestCategory", Unit), new Trait("Class", "Cake"), };
            AssertFindsTestWithTraits("Vec/TypeParameterizedTests/0.ThreeTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/0.ThreeTraits", traits);
            AssertFindsTestWithTraits("Arr/TypeParameterizedTests/1.ThreeTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithOneTrait()
        {
            Trait[] traits = { new Trait("Type", "Small") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits/0 [(1,)]", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithTwoTraits()
        {
            Trait[] traits = { new Trait("Type", "Small"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits2/0 [(1,)]", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithThreeTraits()
        {
            Trait[] traits = { new Trait("Type", "Medium"), new Trait("Author", "MSI"), new Trait("TestCategory", "Integration") };
            AssertFindsTestWithTraits("InstantiationName/ParameterizedTests.SimpleTraits3/0 [(1,)]", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexBeforeFromOptions_AddsTraitIfNotAlreadyExisting()
        {
            string testname = "InstantiationName/ParameterizedTests.Simple/0 [(1,)]";
            Trait[] traits = { };
            AssertFindsTestWithTraits(testname, traits);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape(testname), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexBeforeFromOptions_TraitFromOptionsIsOverridenByTraitFromTest()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "Medium") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_BothRegexesFromOptions_BeforeTraitIsOverridenByAfterTrait()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "BeforeType").Yield().ToList());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "AfterType").Yield().ToList());

            Trait[] traits = { new Trait("Type", "AfterType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexAfterFromOptions_AfterTraitOverridesTraitFromTest()
        {
            Trait[] traits = { new Trait("Type", "Medium") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPassesWithTraits"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPassesWithTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexAfterFromOptions_AddsTraitIfNotAlreadyExisting()
        {
            Trait[] traits = { };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);

            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "SomeNewType").Yield().ToList());

            traits = new[] { new Trait("Type", "SomeNewType") };
            AssertFindsTestWithTraits("TestMath.AddPasses", traits);
        }


        private void AssertFindsTestWithTraits(string displayName, Trait[] traits)
        {
            File.Exists(SampleTestToUse)
                .Should()
                .BeTrue("Build SampleTests in Debug and Release mode before executing this test");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            List<TestCase> tests = discoverer.GetTestsFromExecutable(SampleTestToUse).ToList();

            TestCase testCase = tests.Find(tc => tc.Traits.Count == traits.Length && tc.DisplayName.StartsWith(displayName));
            testCase.Should().NotBeNull();

            foreach (Trait trait in traits)
            {
                Trait foundTrait = testCase.Traits.FirstOrDefault(T => trait.Name == T.Name && trait.Value == T.Value);
                foundTrait.Should().NotBeNull("Didn't find trait: (" + trait.Name + ", " + trait.Value + ")");
            }
        }

    }

}