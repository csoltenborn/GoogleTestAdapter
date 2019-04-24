﻿// This file has been modified by Microsoft on 8/2017.

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
        }

        public void DiscoverTests(IEnumerable<string> executables, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (_settings == null || _settings.GetType() == typeof(SettingsWrapper)) // check whether we have a mock
            {
                CommonFunctions.CreateEnvironment(discoveryContext.RunSettings,
                   logger, out _logger, out _settings);
            }
            _discoverer = new GoogleTestDiscoverer(_logger, _settings);

            if (!IsSupportedVisualStudioVersion())
                return;
            CommonFunctions.LogVisualStudioVersion(_logger);

            _logger.LogInfo(Common.Resources.TestDiscoveryStarting);
            _logger.DebugInfo(String.Format(Resources.Settings, _settings));
            if (_settings.SkipOriginCheck)
                _logger.LogWarning($"Option '{SettingsWrapper.OptionSkipOriginCheck}' is true - this might impose a security risk to your system");

            try
            {
                var reporter = new VsTestFrameworkReporter(discoverySink, _logger);
                _discoverer.DiscoverTests(executables, reporter);

                stopwatch.Stop();
                _logger.LogInfo(String.Format(Resources.TestDiscoveryCompleted, stopwatch.Elapsed));
            }
            catch (Exception e)
            {
                _logger.LogError(String.Format(Resources.TestDiscoveryExceptionError, e));
            }

            CommonFunctions.ReportErrors(_logger, TestPhase.TestDiscovery, _settings.DebugMode);
        }

        private bool IsSupportedVisualStudioVersion()
        {
            var version = VsVersionUtils.GetVisualStudioVersion(_logger);
            switch (version)
            {
                case VsVersion.Unknown:
                    _logger.LogWarning(String.Format(Resources.IdentifyVSError, Common.Resources.ExtensionName));
                    return true;
                case VsVersion.VS2012:
                    _logger.LogError(String.Format(Resources.VS2012Error, Common.Resources.ExtensionName));
                    return false;
                default:
                    return true;
            }
        }

    }

}