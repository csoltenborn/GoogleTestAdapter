using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;
using VSLangProj;

namespace NewProjectWizard.GTA
{
    // ReSharper disable once UnusedMember.Global
    public class GtestProjectWizard : ProjectWizardBase
    {
        private const string GtestIncludePlaceholder = "$gtestinclude$";
        private const string LinkGtestAsDllPlaceholder = "$link_gtest_as_dll$";

        private readonly List<Project> _projectsUnderTest = new List<Project>();
        private Project _gtestProject;

        protected override void RunStarted()
        {
            var gtestProjects = GtestHelper.FindGtestProjects(CppProjects, Logger).ToList();
            // TODO order by toolset?
            var gtestProjectCandidate = gtestProjects.FirstOrDefault();

            using (var dialog = new CreateProjectDialog(CppProjects, gtestProjects)
            {
                GtestProject = gtestProjectCandidate
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    throw new WizardCancelledException();
                }

                _gtestProject = dialog.GtestProject;
                Logger.DebugInfo(_gtestProject != null
                    ? $"Selected gtest project: {_gtestProject.Name} ({_gtestProject.FullName})"
                    : "No gtest project selected");

                _projectsUnderTest.AddRange(dialog.ProjectsUnderTest);
                Logger.DebugInfo($"Projects under test: {string.Join(", ", _projectsUnderTest.Select(p => p.Name))}");
            }

            if (_gtestProject == null && !ContinueWithoutGtestProject())
            {
                throw new WizardCancelledException();
            }

            FillReplacementsDictionary();
        }

        public override void ProjectFinishedGenerating(Project project)
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

        private bool ContinueWithoutGtestProject()
        {
            string message = "No gtest project has been selected, and thus this project will need some extra steps before being compilable (e.g., add Google Test dependency via NuGet). "
                             + "Note that you can create a proper Google Test project by first executing project template 'Google Test DLL'. "
                             + Environment.NewLine
                             + Environment.NewLine
                             + "Continue anyways?";
            string title = "No gtest project selected";
            return MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        }

        private void FillReplacementsDictionary()
        {
            string value = GetPlatformToolset(_projectsUnderTest);
            ReplacementsDictionary.Add(ToolsetPlaceholder, value);
            Logger.DebugInfo($"Platform toolset: '{value}'");

            value = GtestHelper.GetLinkGtestAsDll(_gtestProject);
            ReplacementsDictionary.Add(LinkGtestAsDllPlaceholder, value);
            Logger.DebugInfo($"Link gtest as DLL: '{value}'");

            value = GtestHelper.GetGtestInclude(_gtestProject);
            ReplacementsDictionary.Add(GtestIncludePlaceholder, value);
            Logger.DebugInfo($"Includes folder: '{value}'");
        }

        private void SafeAddProjectReference(Project project, Project referencedProject)
        {
            try
            {
                VSProject vsProj = project.Object as VSProject;
                vsProj?.References.AddProject(referencedProject);
                Logger.DebugInfo($"Project {project.Name}: Added reference to project {referencedProject.Name}");
            }
            catch (Exception ex)
            {
                // Known issue, remove when fixed
                if (!ex.Message.Equals("Error HRESULT E_FAIL has been returned from a call to a COM component."))
                {
                    Logger.LogError($"Exception while adding project {referencedProject.Name} as reference to project {project.Name}. Exception message: {ex.Message}");
                    throw;
                }
            }
        }

    }

}