// This file has been modified by Microsoft on 7/2017.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.TestAdapter
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class TestDiscoverer : ITestDiscoverer
    {
        private ILogger _logger;
        private SettingsWrapper _settings;
        private GoogleTestDiscoverer _discoverer;

        // ReSharper disable once UnusedMember.Global
        public TestDiscoverer() : this(null, null) { }

        public TestDiscoverer(ILogger logger, SettingsWrapper settings)
        {
            _settings = settings;
            _logger = logger;
            _discoverer = new GoogleTestDiscoverer(_logger, _settings);
        }

        public void DiscoverTests(IEnumerable<string> executables, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (_settings == null || _settings.GetType() == typeof(SettingsWrapper)) // check whether we have a mock
            {
                CommonFunctions.CreateEnvironment(discoveryContext.RunSettings,
                   logger, out _logger, out _settings);
                _discoverer = new GoogleTestDiscoverer(_logger, _settings);
            }

            if (!IsSupportedVisualStudioVersion())
                return;
            CommonFunctions.LogVisualStudioVersion(_logger);

            _logger.LogInfo(Strings.Instance.TestDiscoveryStarting);
            _logger.DebugInfo($"Solution settings: {_settings}");

            try
            {
                var reporter = new VsTestFrameworkReporter(discoverySink, _logger);
                _discoverer.DiscoverTests(executables, reporter);

                stopwatch.Stop();
                _logger.LogInfo($"Test discovery completed, overall duration: {stopwatch.Elapsed}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while discovering tests: {e}");
            }

            CommonFunctions.ReportErrors(_logger, "test discovery", _settings.DebugMode);
        }

        private bool IsSupportedVisualStudioVersion()
        {
            var version = VsVersionUtils.GetVisualStudioVersion(_logger);
            switch (version)
            {
                case VsVersion.Unknown:
                    _logger.LogWarning($"Could not identify Visual Studio version. {Strings.Instance.ExtensionName} requires at least Visual Studio 2012 Update 1.");
                    return true;
                case VsVersion.VS2012:
                    _logger.LogError($"{Strings.Instance.ExtensionName} requires at least Visual Studio 2012 Update 1 - please update your Visual Studio installation.");
                    return false;
                default:
                    return true;
            }
        }

    }

}