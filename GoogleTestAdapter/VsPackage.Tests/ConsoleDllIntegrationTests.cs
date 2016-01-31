using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapterUiTests;

namespace GoogleTestAdapter.VsPackage
{

    [TestClass]
    public class ConsoleDllIntegrationTests : AbstractConsoleIntegrationTests
    {

        protected override string GetAdapterIntegration()
        {
            return @"/TestAdapterPath:" + testAdapterDir;
        }

        #region method stubs for code coverage

        [TestMethod]
        [TestCategory("End to end")]
        public override void Console_ListDiscoverers_DiscovererIsListed()
        {
            base.Console_ListDiscoverers_DiscovererIsListed();
        }

        [TestMethod]
        [TestCategory("End to end")]
        public override void Console_ListExecutors_ExecutorIsListed()
        {
            base.Console_ListExecutors_ExecutorIsListed();
        }

        [TestMethod]
        [TestCategory("End to end")]
        public override void Console_ListSettingsProviders_SettingsProviderIsListed()
        {
            base.Console_ListSettingsProviders_SettingsProviderIsListed();
        }

        #endregion

    }

}