using System;
using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestAdapter
{
    public static class CommonFunctions
    {
        public static void ReportErrors(ILogger logger, string phase, bool isDebugModeEnabled)
        {
            IList<string> errors = logger.GetMessages(Severity.Error, Severity.Warning);
            if (errors.Count == 0)
                return;

            bool hasErrors = logger.GetMessages(Severity.Error).Count > 0;
            string hint = isDebugModeEnabled
                ? ""
                : " (enable debug mode for more information)";
            string jointErrors = string.Join(Environment.NewLine, errors);

            string message = $"{Environment.NewLine}================{Environment.NewLine}" 
                + $"The following errors and warnings occured during {phase}{hint}:{Environment.NewLine}" 
                + jointErrors;

            if (hasErrors)
                logger.LogError(message);
            else
                logger.LogWarning(message);
        }

        public static void CreateEnvironment(IRunSettings runSettings, IMessageLogger messageLogger, out ILogger logger, out SettingsWrapper settings)
        {
            var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
            RunSettingsContainer ourRunSettings = settingsProvider != null ? settingsProvider.SettingsContainer : new RunSettingsContainer();
            var settingsWrapper = new SettingsWrapper(ourRunSettings);

            var loggerAdapter = new VsTestFrameworkLogger(messageLogger, () => settingsWrapper.DebugMode, () => settingsWrapper.TimestampOutput);
            var regexParser = new RegexTraitParser(loggerAdapter);
            settingsWrapper.RegexTraitParser = regexParser;

            settings = settingsWrapper;
            logger = loggerAdapter;
        }

        public static void LogVisualStudioVersion(ILogger logger)
        {
            VsVersion version = VsVersionUtils.GetVisualStudioVersion(logger);
            switch (version)
            {
                // warning printed while checking version
                case VsVersion.Unknown:
                case VsVersion.VS2012:
                    return;
                default:
                    logger.DebugInfo($"Visual Studio Version: {version}");
                    break;
            }
        }

    }
}