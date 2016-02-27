using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.VsPackage
{

    [TestClass]
    public class ConsoleDllIntegrationTests : AbstractConsoleIntegrationTests
    {

        protected override string GetAdapterIntegration()
        {
            return @"/TestAdapterPath:" + TestAdapterDir;
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