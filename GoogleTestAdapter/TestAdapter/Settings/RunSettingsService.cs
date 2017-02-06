using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Xml.XPath;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using EnvDTE;
using GoogleTestAdapter.Settings;
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

            if (CopyToUnsetValues(runSettingsNavigator, settingsContainer))
            {
                runSettingsNavigator.DeleteSelf(); // this node is to be replaced by the final run settings
            }

            string solutionRunSettingsFile = GetSolutionSettingsXmlFile();
            try
            {
                if (File.Exists(solutionRunSettingsFile))
                {
                    var solutionRunSettingsDocument = new XPathDocument(solutionRunSettingsFile);
                    XPathNavigator solutionRunSettingsNavigator = solutionRunSettingsDocument.CreateNavigator();
                    if (solutionRunSettingsNavigator.MoveToChild(Constants.RunSettingsName, ""))
                        CopyToUnsetValues(solutionRunSettingsNavigator, settingsContainer);
                }
            }
            catch (Exception e)
            {
                logger.Log(MessageLevel.Warning,
                    $"Solution test settings file could not be parsed, check file: {solutionRunSettingsFile}");
                logger.Log(MessageLevel.Warning, e.ToString());
            }

            GetValuesFromGlobalSettings(settingsContainer);

            runSettingsNavigator.MoveToChild(Constants.RunSettingsName, "");
            runSettingsNavigator.AppendChild(settingsContainer.ToXml().CreateNavigator());

            runSettingsNavigator.MoveToRoot();
            return runSettingsNavigator;
        }

        private void GetValuesFromGlobalSettings(RunSettings settings)
        {
            settings.VisualStudioProcessId = null;
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
                var sourceRunSettings = RunSettingsContainer.LoadFromXml(sourceNavigator.ReadSubtree());
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