using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.TestAdapter
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class TestDiscoverer : ITestDiscoverer
    {
        private TestEnvironment _testEnvironment;
        private GoogleTestDiscoverer _discoverer;

        // ReSharper disable once UnusedMember.Global
        public TestDiscoverer() : this(null) { }

        public TestDiscoverer(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
            _discoverer = new GoogleTestDiscoverer(_testEnvironment);
        }


        public void DiscoverTests(IEnumerable<string> executables, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            if (_testEnvironment == null || _testEnvironment.Options.GetType() == typeof(SettingsWrapper)) // check whether we have a mock
            {
                _testEnvironment = TestExecutor.CreateTestEnvironment(discoveryContext.RunSettings, logger);
                _discoverer = new GoogleTestDiscoverer(_testEnvironment);
            }

            try
            {
                VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(discoverySink);
                _discoverer.DiscoverTests(executables, reporter);
            }
            catch (Exception e)
            {
                _testEnvironment.LogError("Exception while discovering tests: " + e);
            }

        }

    }

}