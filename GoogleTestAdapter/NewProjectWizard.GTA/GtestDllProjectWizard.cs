using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    public class GtestDllProjectWizard : ProjectWizardBase
    {
        public override void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, 
            WizardRunKind runKind, object[] customParams)
        {
            _DTE dte = (_DTE) automationObject;
            var cppProjects = dte.Solution.Projects.Cast<Project>().Where(p => p.IsCppProject()).ToList();

            string value = GetPlatformToolset(cppProjects);
            replacementsDictionary.Add(ToolsetPlaceholder, value);
            Logger.DebugInfo($"Toolset: '{value}'");

            FillReplacementDirectory(replacementsDictionary);
        }
    }
}