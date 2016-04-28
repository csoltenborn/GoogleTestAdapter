using System;
using System.Collections.Generic;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
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

            if (TestEnvironment == null || TestEnvironment.Options.GetType() == typeof(SettingsWrapper)) // check whether we have a mock
            {
                var settingsProvider = discoveryContext.RunSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                RunSettings ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();

                TestEnvironment = new TestEnvironment(new SettingsWrapper(ourRunSettings, loggerAdapter), loggerAdapter);
                Discoverer = new GoogleTestDiscoverer(TestEnvironment);
            }

            try
            {
                VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(discoverySink);
                Discoverer.DiscoverTests(executables, reporter);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while discovering tests: " + e);
            }

        }

    }

}