// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Xml.XPath;
using Constants = Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [Export(typeof(IRunSettingsService))]
    [SettingsName(GoogleTestConstants.SettingsName)]
    public class RunSettingsService : IRunSettingsService
    {
        public string Name => GoogleTestConstants.SettingsName;

        private readonly IGlobalRunSettings _globalRunSettings;

        [ImportingConstructor]
        public RunSettingsService([Import(typeof(IGlobalRunSettings))] IGlobalRunSettings globalRunSettings)
        {
            _globalRunSettings = globalRunSettings;
        }

        public IXPathNavigable AddRunSettings(IXPathNavigable runSettingDocument,
            IRunSettingsConfigurationInfo configurationInfo, ILogger logger)
        {
            XPathNavigator runSettingsNavigator = runSettingDocument.CreateNavigator();
            Debug.Assert(runSettingsNavigator != null, "userRunSettingsNavigator == null!");
            if (!runSettingsNavigator.MoveToChild(Constants.RunSettingsName, ""))
            {
                logger.Log(MessageLevel.Warning, "RunSettingsDocument does not contain a RunSettings node! Canceling settings merging...");
                return runSettingsNavigator;
            }

            var settingsContainer = new RunSettingsContainer();

            try
            {
                if (settingsContainer.GetUnsetValuesFrom(runSettingsNavigator))
                {
                    runSettingsNavigator.DeleteSelf(); // this node is to be replaced by the final run settings
                }
            }
            catch (InvalidRunSettingsException e)
            {
                logger.Log(MessageLevel.Error, $"Invalid run settings: {e.Message}");
            }

            GetValuesFromSolutionSettingsFile(settingsContainer, logger);

            foreach (var projectSettings in settingsContainer.ProjectSettings)
            {
                projectSettings.GetUnsetValuesFrom(settingsContainer.SolutionSettings);
            }

            GetValuesFromGlobalSettings(settingsContainer, logger);

            runSettingsNavigator.MoveToChild(Constants.RunSettingsName, "");
            runSettingsNavigator.AppendChild(settingsContainer.ToXml().CreateNavigator());

            ConfigureCodeCoverage(runSettingDocument, logger);

            runSettingsNavigator.MoveToRoot();
            return runSettingsNavigator;
        }

        // an exception would make Test Explorer drop all settings provided by GTA
        private static void ConfigureCodeCoverage(IXPathNavigable runSettingDocument, ILogger logger)
        {
            try
            {
                // the code coverage of VS 2022 and later knows dynamic native instrumentation
                bool enableDynamicNativeInstrumentation = VsVersionUtils.VsVersion >= VsVersion.VS2022;
                if (CodeCoverageRunSettings.Configure(runSettingDocument, enableDynamicNativeInstrumentation))
                    logger.Log(MessageLevel.Informational, "Google Test Adapter: adjusted code coverage settings to Google Test executables");
            }
            catch (Exception e)
            {
                logger.Log(MessageLevel.Warning, $"Google Test Adapter: could not adjust code coverage settings: {e.Message}");
            }
        }

        private void GetValuesFromSolutionSettingsFile(RunSettingsContainer settingsContainer, ILogger logger)
        {
            string solutionRunSettingsFile = GetSolutionSettingsXmlFile();
            try
            {
                if (solutionRunSettingsFile != null && File.Exists(solutionRunSettingsFile))
                {
                    if (!settingsContainer.GetUnsetValuesFrom(solutionRunSettingsFile))
                    {
                        logger.Log(MessageLevel.Warning,
                            $"Solution test settings file found at '{solutionRunSettingsFile}', but does not contain {Constants.RunSettingsName} node");
                    }
                }
            }
            catch (InvalidRunSettingsException e) when (e.InnerException != null)
            {
                logger.Log(MessageLevel.Error,
                    $"Solution test settings file could not be parsed, check file '{solutionRunSettingsFile}'. Error message: {e.InnerException.Message}");
            }
            catch (Exception e)
            {
                logger.Log(MessageLevel.Error,
                    $"Solution test settings file could not be parsed, check file '{solutionRunSettingsFile}'. Exception:{Environment.NewLine}{e}");
            }
        }

        private void GetValuesFromGlobalSettings(RunSettingsContainer settingsContainer, ILogger logger)
        {
            // the global settings are only updated if GTA's options change, but the solution
            // and its active configuration might have changed in the meantime
            var visualStudioConfiguration = GetVisualStudioConfiguration(logger);

            GetValuesFromGlobalSettings(settingsContainer.SolutionSettings, visualStudioConfiguration);
            foreach (RunSettings projectSettings in settingsContainer.ProjectSettings)
            {
                GetValuesFromGlobalSettings(projectSettings, visualStudioConfiguration);
            }
        }

        private void GetValuesFromGlobalSettings(RunSettings settings, VisualStudioConfiguration visualStudioConfiguration)
        {
            // these settings must not be provided through runsettings files. If they still
            // are, the following makes sure that they are ignored
            settings.SkipOriginCheck = null;

            // internal
            settings.DebuggingNamedPipeId = null;
            settings.SolutionDir = visualStudioConfiguration?.SolutionDir;
            settings.PlatformName = visualStudioConfiguration?.PlatformName;
            settings.ConfigurationName = visualStudioConfiguration?.ConfigurationName;

            settings.GetUnsetValuesFrom(_globalRunSettings.RunSettings);
        }

        // This is called on background threads and must not wait for the UI thread, which might be busy (e.g. loading
        // the solution) or even be waiting for the caller; the configuration is thus taken as of the last change.
        // protected for testing
        protected virtual VisualStudioConfiguration GetVisualStudioConfiguration(ILogger logger)
        {
            VisualStudioConfigurationTracker.RequestUpdate();
            return VisualStudioConfigurationTracker.Current;
        }

        // protected for testing
        protected virtual string GetSolutionSettingsXmlFile()
        {
            return SolutionPaths.GetSolutionSettingsFile(VisualStudioConfigurationTracker.Current?.SolutionFullName);
        }

    }

}