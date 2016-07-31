using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage
{

    [TestClass]
    public class ConsoleDllIntegrationTests : AbstractConsoleIntegrationTests
    {

        protected override string GetAdapterIntegration()
        {
            return GetLogger() + @"/TestAdapterPath:" + TestAdapterDir;
        }

        #region method stubs for code coverage

        [TestMethod]
        [TestCategory(EndToEnd)]
        public override void Console_ListDiscoverers_DiscovererIsListed()
        {
            base.Console_ListDiscoverers_DiscovererIsListed();
        }

        [TestMethod]
        [TestCategory(EndToEnd)]
        public override void Console_ListExecutors_ExecutorIsListed()
        {
            base.Console_ListExecutors_ExecutorIsListed();
        }

        [TestMethod]
        [TestCategory(EndToEnd)]
        public override void Console_ListSettingsProviders_SettingsProviderIsListed()
        {
            base.Console_ListSettingsProviders_SettingsProviderIsListed();
        }

        #endregion

    }

}