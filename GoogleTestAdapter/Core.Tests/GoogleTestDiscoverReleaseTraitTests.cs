using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestDiscovererReleaseTraitTests : GoogleTestDiscovererTraitTestsBase
    {
        protected override string SampleTestToUse => TestResources.Tests_ReleaseX86;

        #region Method stubs for code coverage

        [TestMethod]
        public override void GetTestsFromExecutable_RegexAfterFromOptions_AddsTraitIfNotAlreadyExisting()
        {
            base.GetTestsFromExecutable_RegexAfterFromOptions_AddsTraitIfNotAlreadyExisting();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexButNoSourceLocation_TraitsAreAdded()
        {
            base.GetTestsFromExecutable_RegexButNoSourceLocation_TraitsAreAdded();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_BothRegexesFromOptions_BeforeTraitIsOverridenByAfterTrait()
        {
            base.GetTestsFromExecutable_BothRegexesFromOptions_BeforeTraitIsOverridenByAfterTrait();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexAfterFromOptions_AfterTraitOverridesTraitFromTest()
        {
            base.GetTestsFromExecutable_RegexAfterFromOptions_AfterTraitOverridesTraitFromTest();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexBeforeFromOptions_AddsTraitIfNotAlreadyExisting()
        {
            base.GetTestsFromExecutable_RegexBeforeFromOptions_AddsTraitIfNotAlreadyExisting();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexBeforeFromOptions_TraitFromOptionsIsOverridenByTraitFromTest()
        {
            base.GetTestsFromExecutable_RegexBeforeFromOptions_TraitFromOptionsIsOverridenByTraitFromTest();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithOneTrait()
        {
            base.GetTestsFromExecutable_SampleTests_FindsFixtureTestWithOneTrait();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithThreeTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsFixtureTestWithThreeTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithTwoTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsFixtureTestWithTwoTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsAllAmountsOfTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsAllAmountsOfTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithOneTrait()
        {
            base.GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithOneTrait();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithThreeTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithThreeTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithTwoTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithTwoTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithOneTrait()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithOneTrait();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithThreeTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithThreeTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithTwoTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTypeParameterizedTestWithTwoTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTypedTestWithOneTrait()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTypedTestWithOneTrait();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTypedTestWithThreeTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTypedTestWithThreeTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTypedTestWithTwoTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTypedTestWithTwoTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTestWithTwoEqualTraits()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTestWithTwoEqualTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexBeforeFromOptions_FindsTestWithTwoEqualTraits()
        {
            base.GetTestsFromExecutable_RegexBeforeFromOptions_FindsTestWithTwoEqualTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexBeforeFromOptionsThreeEqualTraits_FindsTestWithTwoEqualTraits()
        {
            base.GetTestsFromExecutable_RegexBeforeFromOptionsThreeEqualTraits_FindsTestWithTwoEqualTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexAfterFromOptionsOneEqualTrait_FindsTestTestWithOneEqualTrait()
        {
            base.GetTestsFromExecutable_RegexAfterFromOptionsOneEqualTrait_FindsTestTestWithOneEqualTrait();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexAfterFromOptionsTwoEqualTraits_FindsTestWithTwoEqualTraits()
        {
            base.GetTestsFromExecutable_RegexAfterFromOptionsTwoEqualTraits_FindsTestWithTwoEqualTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_RegexBeforeFromOptionsTwoEqualTraits_FindsTestWithTwoAndTwoEqualTraits()
        {
            base.GetTestsFromExecutable_RegexBeforeFromOptionsTwoEqualTraits_FindsTestWithTwoAndTwoEqualTraits();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsTestWithUmlauts()
        {
            base.GetTestsFromExecutable_SampleTests_FindsTestWithUmlauts();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsFixtureTestWithUmlauts()
        {
            base.GetTestsFromExecutable_SampleTests_FindsFixtureTestWithUmlauts();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithUmlauts()
        {
            base.GetTestsFromExecutable_SampleTests_FindsParameterizedTestWithUmlauts();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_NewExecutionEnvironmentAndFailUserParamIsSet_NoTestsAreFound()
        {
            base.GetTestsFromExecutable_NewExecutionEnvironmentAndFailUserParamIsSet_NoTestsAreFound();
        }

        [TestMethod]
        public override void GetTestsFromExecutable_OldExecutionEnvironmentAndFailUserParamIsSet_NoTestsAreFound()
        {
            base.GetTestsFromExecutable_OldExecutionEnvironmentAndFailUserParamIsSet_NoTestsAreFound();
        }

        #endregion

    }

}