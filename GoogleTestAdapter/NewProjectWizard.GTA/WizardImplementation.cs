using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using VSLangProj;

namespace NewProjectWizard.GTA
{
    public class WizardImplementation : IWizard
    {
        private readonly List<Project> _projectsUnderTest = new List<Project>();

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, 
            WizardRunKind runKind, object[] customParams)
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));

            var cppProjects = dte.Solution.Projects.Cast<Project>().Where(p => p.IsCppProject());
            using (var wizard = new SinglePageWizard(cppProjects))
            {
                var result = wizard.ShowDialog();
                if (result != DialogResult.OK)
                {
                    throw new WizardCancelledException();
                }

                _projectsUnderTest.AddRange(wizard.SelectedProjects);
            }
        }

        public void ProjectFinishedGenerating(Project project)
        {
            try
            {
                VSProject vsProj = project.Object as VSProject;
                foreach (Project referencedProject in _projectsUnderTest)
                {
                    vsProj?.References.AddProject(referencedProject);
                }
            }
            catch (Exception ex)
            {
                // Known issue, remove when fixed
                if (!ex.Message.Equals("Error HRESULT E_FAIL has been returned from a call to a COM component."))
                    throw;
            }
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