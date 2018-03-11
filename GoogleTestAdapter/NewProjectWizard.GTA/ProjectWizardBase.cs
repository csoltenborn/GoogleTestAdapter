using System.Collections.Generic;
using EnvDTE;
using GoogleTestAdapter.Common;
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

        protected ProjectWizardBase()
        {
            Logger = new TestWindowLogger();
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