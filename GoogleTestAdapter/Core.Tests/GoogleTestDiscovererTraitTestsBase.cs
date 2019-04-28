using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter
{
    [TestClass]
    public abstract class GoogleTestDiscovererTraitTestsBase : TestsBase
    {
        protected abstract string SampleTestToUse { get; }


        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsAllAmountsOfTraits()
        {
            var traits = new List<Trait>();
            for (int i = 1; i <= 8; i++)
            {
                traits.Add(new Trait($"Trait{i}", $"Equals{i}"));
                AssertFindsTestWithTraits($"Traits.With{i}Traits", traits.ToArray());
            }
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
        public virtual void GetTestsFromExecutable_SampleTests_FindsTestWithUmlauts()
        {
            Trait[] traits = { new Trait("Träit1", "Völue1a"), new Trait("Träit1", "Völue1b"), new Trait("Träit2", "Völue2") };
            AssertFindsTestWithTraits("Ümlautß.Träits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithUmlauts()
        {
            Trait[] traits = { new Trait("Träit1", "Völue1a"), new Trait("Träit1", "Völue1b"), new Trait("Träit2", "Völue2") };
            AssertFindsTestWithTraits("ÜnstanceName/ParameterizedTästs.Träits/0 [(1,ÄÖÜäöüß)]", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithUmlauts()
        {
            Trait[] traits = { new Trait("Träit1", "Völue1a"), new Trait("Träit1", "Völue1b"), new Trait("Träit2", "Völue2") };
            AssertFindsTestWithTraits("TheFixtüre.Träits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_SampleTests_FindsTestWithTwoEqualTraits()
        {
            Trait[] traits = { new Trait("Author", "JOG"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits("Traits.WithEqualTraits", traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexBeforeFromOptions_FindsTestWithTwoEqualTraits()
        {
            string testname = "Traits.WithEqualTraits";
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new RegexTraitPair(Regex.Escape(testname), "Author", "Foo").Yield().ToList());

            Trait[] traits = { new Trait("Author", "JOG"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexBeforeFromOptionsThreeEqualTraits_FindsTestWithTwoEqualTraits()
        {
            string testname = "Traits.WithEqualTraits";
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(
                new List<RegexTraitPair>
                {
                    new RegexTraitPair(Regex.Escape(testname), "Author", "Foo"),
                    new RegexTraitPair(Regex.Escape(testname), "Author", "Bar"),
                    new RegexTraitPair(Regex.Escape(testname), "Author", "Baz")
                });

            Trait[] traits = { new Trait("Author", "JOG"), new Trait("Author", "CSO") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexAfterFromOptionsOneEqualTrait_FindsTestTestWithOneEqualTrait()
        {
            string testname = "Traits.WithEqualTraits";
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(
                new List<RegexTraitPair>
                {
                    new RegexTraitPair(Regex.Escape(testname), "Author", "Foo")
                });

            Trait[] traits = { new Trait("Author", "Foo") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexAfterFromOptionsTwoEqualTraits_FindsTestWithTwoEqualTraits()
        {
            string testname = "Traits.WithEqualTraits";
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(
                new List<RegexTraitPair>
                {
                    new RegexTraitPair(Regex.Escape(testname), "Author", "Foo"),
                    new RegexTraitPair(Regex.Escape(testname), "Author", "Bar")
                });

            Trait[] traits = { new Trait("Author", "Foo"), new Trait("Author", "Bar") };
            AssertFindsTestWithTraits(testname, traits);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_RegexBeforeFromOptionsTwoEqualTraits_FindsTestWithTwoAndTwoEqualTraits()
        {
            string testname = "Traits.WithEqualTraits";
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(
                new List<RegexTraitPair>
                {
                    new RegexTraitPair(Regex.Escape(testname), "Author2", "Foo"),
                    new RegexTraitPair(Regex.Escape(testname), "Author2", "Bar")
                });

            Trait[] traits = {
                new Trait("Author", "JOG"),
                new Trait("Author", "CSO") ,
                new Trait("Author2", "Foo"),
                new Trait("Author2", "Bar") };
            AssertFindsTestWithTraits(testname, traits);
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

        [TestMethod]
        [TestCategory(Integration)]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public virtual void GetTestsFromExecutable_RegexButNoSourceLocation_TraitsAreAdded()
        {
            string pdb = Path.ChangeExtension(SampleTestToUse, "pdb");
            pdb.AsFileInfo().Should().Exist();
            string tempFile = Path.ChangeExtension(pdb, "gtatmpext");
            tempFile.AsFileInfo().Should().NotExist();

            try
            {
                File.Move(pdb, tempFile);
                pdb.AsFileInfo().Should().NotExist();

                MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new RegexTraitPair(Regex.Escape("TestMath.AddPasses"), "Type", "SomeNewType").Yield().ToList());

                var traits = new[] { new Trait("Type", "SomeNewType") };
                AssertFindsTestWithTraits("TestMath.AddPasses", traits);
            }
            finally
            {
                File.Move(tempFile, pdb);
                pdb.AsFileInfo().Should().Exist();
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_NewExecutionEnvironmentAndFailUserParamIsSet_NoTestsAreFound()
        {
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.Native);

            CheckEffectOfDiscoveryParam();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void GetTestsFromExecutable_OldExecutionEnvironmentAndFailUserParamIsSet_NoTestsAreFound()
        {
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);

            CheckEffectOfDiscoveryParam();
        }


        private void AssertFindsTestWithTraits(string displayName, Trait[] traits)
        {
            SampleTestToUse.AsFileInfo()
                .Should().Exist("building the SampleTests solution produces that executable");

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            List<TestCase> tests = discoverer.GetTestsFromExecutable(SampleTestToUse).ToList();

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockLogger.Verify(l => l.DebugError(It.IsAny<string>()), Times.Never);
            tests.Should().NotBeEmpty();

            TestCase testCase = tests.Find(tc => tc.Traits.Count == traits.Length && tc.DisplayName.StartsWith(displayName));
            testCase.Should().NotBeNull($"Test should exist: {displayName}, {traits.Length} trait(s)");

            foreach (Trait trait in traits)
            {
                Trait foundTrait = testCase.Traits.FirstOrDefault(T => trait.Name == T.Name && trait.Value == T.Value);
                foundTrait.Should().NotBeNull("Didn't find trait: (" + trait.Name + ", " + trait.Value + ")");
            }
        }

        private void CheckEffectOfDiscoveryParam()
        {
            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            var tests = discoverer.GetTestsFromExecutable(SampleTestToUse);

            tests.Should().NotBeEmpty();

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-justfail");

            discoverer = new GoogleTestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            tests = discoverer.GetTestsFromExecutable(SampleTestToUse);

            tests.Should().BeEmpty();
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("failed intentionally"))), Times.AtLeastOnce);
        }

    }

}