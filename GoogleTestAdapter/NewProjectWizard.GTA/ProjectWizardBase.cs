using System.Collections.Generic;
using EnvDTE;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    public abstract class ProjectWizardBase : IWizard
    {
        protected const string ToolsetPlaceholder = "$gta_toolset$";
        protected const string GenerateDebugInformationPlaceholder = "$gta_generate_debug_information$";
        protected const string VariadicMaxPlaceholder = "$gta_variadic_max$";

        protected ILogger Logger { get; }
        protected SettingsWrapper Settings { get; }

        protected ProjectWizardBase()
        {
            Logger = new TestWindowLogger(
                () => Settings?.DebugMode ?? SettingsWrapper.OptionDebugModeDefaultValue, 
                () => Settings?.TimestampOutput ?? SettingsWrapper.OptionTimestampOutputDefaultValue);
            Settings = CreateSettings(Logger);

            Logger.DebugInfo($"VS version: '{VisualStudioHelper.GetVisualStudioVersionString()}'");
        }

        public abstract void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind,
            object[] customParams);

        protected void FillReplacementDirectory(Dictionary<string, string> replacementsDictionary)
        {
            string value = VisualStudioHelper.GetGenerateDebugInformationFromVisualStudioVersion();
            replacementsDictionary.Add(GenerateDebugInformationPlaceholder, value);
            Logger.DebugInfo($"GenerateDebugInfo: '{value}'");

            value = VisualStudioHelper.GetVariadicMaxFromVisualStudioVersion();
            replacementsDictionary.Add(VariadicMaxPlaceholder, value);
            Logger.DebugInfo($"VariadixMax: '{value}'");
        }

        private static SettingsWrapper CreateSettings(ILogger logger)
        {
            var componentModel = (IComponentModel) Package.GetGlobalService(typeof(SComponentModel));
            var settings = componentModel.GetService<IGlobalRunSettingsInternal>();

            var settingsContainer = new RunSettingsContainer
            {
                SolutionSettings = settings.RunSettings
            };

            return new SettingsWrapper(settingsContainer)
            {
                RegexTraitParser = new RegexTraitParser(logger)
            };
        }

        public virtual void ProjectFinishedGenerating(Project project)
        {
        }

        public virtual void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public virtual void RunFinished()
        {
        }

        #region project item specific
        public virtual bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public virtual void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }
        #endregion

    }

}