// This file has been modified by Microsoft on 6/2017.

using EnvDTE;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Xml;
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
            settingsContainer.SolutionSettings = new RunSettings();

            try
            {
                if (CopyToUnsetValues(runSettingsNavigator, settingsContainer))
                {
                    runSettingsNavigator.DeleteSelf(); // this node is to be replaced by the final run settings
                }
            }
            catch (InvalidRunSettingsException e)
            {
                logger.Log(MessageLevel.Error, $"Invalid run settings: {e.Message}");
            }

            string solutionRunSettingsFile = GetSolutionSettingsXmlFile();
            try
            {
                if (File.Exists(solutionRunSettingsFile))
                {
                    var settings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
                    settings.XmlResolver = null;
                    using (var reader = XmlReader.Create(solutionRunSettingsFile, settings))
                    {
                        var solutionRunSettingsDocument = new XPathDocument(reader);
                        XPathNavigator solutionRunSettingsNavigator = solutionRunSettingsDocument.CreateNavigator();
                        if (solutionRunSettingsNavigator.MoveToChild(Constants.RunSettingsName, ""))
                        {
                            CopyToUnsetValues(solutionRunSettingsNavigator, settingsContainer);
                        }
                        else
                        {
                            logger.Log(MessageLevel.Warning, $"Solution test settings file found at '{solutionRunSettingsFile}', but does not contain {Constants.RunSettingsName} node");
                        }
		    }
                }
            }
            catch (Exception e)
            {
                logger.Log(MessageLevel.Warning,
                    $"Solution test settings file could not be parsed, check file: {solutionRunSettingsFile}{Environment.NewLine}Exception: {e}");
            }

            foreach (var projectSettings in settingsContainer.ProjectSettings)
            {
                projectSettings.GetUnsetValuesFrom(settingsContainer.SolutionSettings);
            }

            GetValuesFromGlobalSettings(settingsContainer);

            runSettingsNavigator.MoveToChild(Constants.RunSettingsName, "");
            runSettingsNavigator.AppendChild(settingsContainer.ToXml().CreateNavigator());

            runSettingsNavigator.MoveToRoot();
            return runSettingsNavigator;
        }

        private void GetValuesFromGlobalSettings(RunSettings settings)
        {
            // these settings must not be provided through runsettings files. If they still
            // are, the following makes sure that they are ignored
            settings.DebuggingNamedPipeId = null;
            settings.SkipOriginCheck = null;

            settings.GetUnsetValuesFrom(_globalRunSettings.RunSettings);
        }

        private void GetValuesFromGlobalSettings(RunSettingsContainer settingsContainer)
        {
            GetValuesFromGlobalSettings(settingsContainer.SolutionSettings);
            foreach (RunSettings projectSettings in settingsContainer.ProjectSettings)
            {
                GetValuesFromGlobalSettings(projectSettings);
            }
        }

        private bool CopyToUnsetValues(XPathNavigator sourceNavigator, RunSettingsContainer targetSettingsContainer)
        {
            if (sourceNavigator.MoveToChild(GoogleTestConstants.SettingsName, ""))
            {
                var sourceRunSettings = RunSettingsContainer.LoadFromXml(sourceNavigator);
                targetSettingsContainer.GetUnsetValuesFrom(sourceRunSettings);
                return true;
            }

            return false;
        }

        // protected for testing
        protected virtual string GetSolutionSettingsXmlFile()
        {
            DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            Debug.Assert(dte != null, "dte == null!");
            return Path.ChangeExtension(dte.Solution.FullName, GoogleTestConstants.SettingsExtension);
        }

    }

}