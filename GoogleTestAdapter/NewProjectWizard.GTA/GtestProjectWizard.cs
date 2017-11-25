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
    public class GtestProjectWizard : IWizard
    {
        private const string GtestIncludePlaceholder = "$gtestinclude$";
        private const string LinkGtestAsDllPlaceholder = "$link_gtest_as_dll$";

        private readonly List<Project> _projectsUnderTest = new List<Project>();
        private Project _gtestProject;

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, 
            WizardRunKind runKind, object[] customParams)
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            var cppProjects = dte.Solution.Projects.Cast<Project>().Where(p => p.IsCppProject()).ToList();

            var gtestProjects = GtestHelper.FindGtestProjects(cppProjects).ToList();
            _gtestProject = gtestProjects.FirstOrDefault();
            if (_gtestProject == null && !ContinueWithoutGtestProject())
            {
                throw new WizardCancelledException();
            }
            cppProjects.RemoveAll(p => gtestProjects.Contains(p));

            replacementsDictionary.Add(GtestIncludePlaceholder, GtestHelper.GetGtestInclude(_gtestProject));
            replacementsDictionary.Add(LinkGtestAsDllPlaceholder, GtestHelper.GetLinkGtestAsDll(_gtestProject));

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
            if (_gtestProject != null)
            {
                SafeAddProjectReference(project, _gtestProject);
            }
            foreach (Project projectUnderTest in _projectsUnderTest)
            {
                SafeAddProjectReference(project, projectUnderTest);
            }
        }

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        private bool ContinueWithoutGtestProject()
        {
            string message = "No gtest project has been found, and thus this project will need some extra steps before being compilable (e.g., add Google Test dependency via NuGet). "
                             + "Note that you can create a proper Google Test project by first executing project template 'Google Test DLL'. "
                             + Environment.NewLine
                             + Environment.NewLine
                             + "Continue anyways?";
            string title = "No gtest project found";
            return MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        }

        private void SafeAddProjectReference(Project project, Project referencedProject)
        {
            try
            {
                VSProject vsProj = project.Object as VSProject;
                vsProj?.References.AddProject(referencedProject);
            }
            catch (Exception ex)
            {
                // Known issue, remove when fixed
                if (!ex.Message.Equals("Error HRESULT E_FAIL has been returned from a call to a COM component."))
                    throw;
            }
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