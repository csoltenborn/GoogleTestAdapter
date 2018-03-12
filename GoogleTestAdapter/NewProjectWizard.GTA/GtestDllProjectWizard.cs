using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    public class GtestDllProjectWizard : ProjectWizardBase
    {
        public override void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, 
            WizardRunKind runKind, object[] customParams)
        {
            string value = VisualStudioHelper.GetPlatformToolsetFromVisualStudioVersion();
            replacementsDictionary.Add(ToolsetPlaceholder, value);
            Logger.DebugInfo($"Toolset: '{value}'");

            FillReplacementDirectory(replacementsDictionary);
        }
    }
}