using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    public class GtestDllProjectWizard : IWizard
    {
        private const string ToolsetPlaceholder = "$gta_toolset$";
        private const string GenerateDebugInformationPlaceholder = "$gta_generate_debug_information$";
        private const string VariadicMaxPlaceholder = "$gta_variadic_max$";

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, 
            WizardRunKind runKind, object[] customParams)
        {
            replacementsDictionary.Add(ToolsetPlaceholder, VisualStudioHelper.GetPlatformToolsetFromVisualStudioVersion());
            replacementsDictionary.Add(GenerateDebugInformationPlaceholder, VisualStudioHelper.GetGenerateDebugInformationFromVisualStudioVersion());
            replacementsDictionary.Add(VariadicMaxPlaceholder, VisualStudioHelper.GetVariadicMaxFromVisualStudioVersion());
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        #region project item specific
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }
        #endregion

    }

}