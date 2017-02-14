using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestDiscovererDebugTraitTests : GoogleTestDiscovererTraitTestsBase
    {
        protected override string SampleTestToUse => TestResources.SampleTests;

        #region Method stubs for code coverage

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

        #endregion

    }
}