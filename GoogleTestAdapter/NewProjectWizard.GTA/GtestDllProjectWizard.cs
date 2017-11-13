using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;

namespace NewProjectWizard.GTA
{
    public class GtestDllProjectWizard : IWizard
    {
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, 
            WizardRunKind runKind, object[] customParams)
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));

            var cppProjects = dte.Solution.Projects.Cast<Project>().Where(p => p.IsCppProject()).ToList();

            var gtestProject = GtestHelper.FindGtestProject(cppProjects);
            if (gtestProject != null)
            {
                string message = "'gtest' project can not be created because a project named 'gtest' already exists in this solution.";
                string title = "'gtest' project already exists";
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new WizardCancelledException();
            }
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