using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapterUiTests;

namespace GoogleTestAdapter.VsPackage
{

    [TestClass]
    public class ConsoleVsixIntegrationTests : AbstractConsoleIntegrationTests
    {

        [ClassInitialize]
        public static void InstallVsix(TestContext testContext)
        {
            VS.SetupVanillaVsExperimentalInstance("");
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            VS.CleanVsExperimentalInstance();
        }

        protected override string GetAdapterIntegration()
        {
            return @"/UseVsixExtensions:true";
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