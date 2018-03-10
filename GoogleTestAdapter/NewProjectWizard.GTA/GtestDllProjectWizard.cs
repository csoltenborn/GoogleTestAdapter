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
            replacementsDictionary.Add(ToolsetPlaceholder, VisualStudioHelper.GetPlatformToolsetFromVisualStudioVersion());
            FillReplacementDirectory(replacementsDictionary);
        }
    }
}