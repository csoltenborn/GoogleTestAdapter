using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.VS.Framework;
using GoogleTestAdapter.VS.Helpers;
using GoogleTestAdapter.VS.Settings;

namespace GoogleTestAdapter.VS
{
    [DefaultExecutorUri(GoogleTestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class TestDiscoverer : ITestDiscoverer
    {
        private TestEnvironment TestEnvironment { get; set; }
        private GoogleTestDiscoverer Discoverer { get; set; }

        public TestDiscoverer() : this(null) { }

        public TestDiscoverer(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
            Discoverer = new GoogleTestDiscoverer(TestEnvironment);
        }


        public void DiscoverTests(IEnumerable<string> executables, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            ILogger loggerAdapter = new VsTestFrameworkLogger(logger);

            if (TestEnvironment == null || TestEnvironment.Options.GetType() == typeof(Options)) // check whether we have a mock
            {
                var settingsProvider = discoveryContext.RunSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                RunSettings ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();

                TestEnvironment = new TestEnvironment(new Options(ourRunSettings, loggerAdapter), loggerAdapter);
                Discoverer = new GoogleTestDiscoverer(TestEnvironment);
            }

            new DebugHelper(TestEnvironment).CheckDebugModeForDiscoveryCode();

            VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(discoverySink, null, TestEnvironment);
            Discoverer.DiscoverTests(executables, loggerAdapter, reporter);
        }

    }

}