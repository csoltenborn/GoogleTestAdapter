using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;
using VSLangProj;

namespace NewProjectWizard.GTA
{
    public class GtestProjectWizard : IWizard
    {
        private const string ToolsetPlaceholder = "$gta_toolset$";
        private const string GenerateDebugInformationPlaceholder = "$gta_generate_debug_information$";
        private const string VariadicMaxPlaceholder = "$gta_variadic_max$";
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

            using (var wizard = new SinglePageWizard(cppProjects))
            {
                var result = wizard.ShowDialog();
                if (result != DialogResult.OK)
                {
                    throw new WizardCancelledException();
                }

                _projectsUnderTest.AddRange(wizard.SelectedProjects);
            }

            replacementsDictionary.Add(ToolsetPlaceholder, GetPlatformToolset(_projectsUnderTest));
            replacementsDictionary.Add(GenerateDebugInformationPlaceholder, VisualStudioHelper.GetGenerateDebugInformationFromVisualStudioVersion());
            replacementsDictionary.Add(VariadicMaxPlaceholder, VisualStudioHelper.GetVariadicMaxFromVisualStudioVersion());
            replacementsDictionary.Add(GtestIncludePlaceholder, GtestHelper.GetGtestInclude(_gtestProject));
            replacementsDictionary.Add(LinkGtestAsDllPlaceholder, GtestHelper.GetLinkGtestAsDll(_gtestProject));
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

        private string GetPlatformToolset(IEnumerable<Project> projectsUnderTest)
        {
            try
            {
                var platformToolset = GetPlatformToolsetFromProjects(projectsUnderTest);
                if (platformToolset != null)
                {
                    return platformToolset;
                }
            }
            catch
            {
                // fallback to toolset by VS version
            }

            return VisualStudioHelper.GetPlatformToolsetFromVisualStudioVersion();
        }

        private string GetPlatformToolsetFromProjects(IEnumerable<Project> projectsUnderTest)
        {
            var toolsetsInUse = new HashSet<string>();
            foreach (Project project in projectsUnderTest)
            {
                XmlDocument projectFile = new XmlDocument();
                projectFile.Load(project.FullName);
                var nodes = projectFile.GetElementsByTagName("PlatformToolset");
                foreach (XmlNode node in nodes)
                {
                    toolsetsInUse.Add(node.InnerText);
                }
            }

            if (toolsetsInUse.Count > 0)
            {
                var comparer = new ToolsetComparer();
                string toolset = toolsetsInUse.OrderByDescending(ts => ts, comparer).First();
                if (comparer.IsKnownToolset(toolset))
                {
                    return toolset;
                }
            }

            return null;
        }

        private class ToolsetComparer : IComparer<string>
        {
            private const int UnknownToolset = -1;

            public int Compare(string x, string y)
            {
                return GetToolsetIndex(x).CompareTo(GetToolsetIndex(y));
            }

            public bool IsKnownToolset(string toolset)
            {
                return GetToolsetIndex(toolset) != UnknownToolset;
            }

            private int GetToolsetIndex(string toolset)
            {
                switch (toolset)
                {
                    case "v100": return 100;
                    case "v100_xp": return 200;
                    case "v110": return 300;
                    case "v110_xp": return 400;
                    case "v120": return 500;
                    case "v120_xp": return 600;
                    case "v140": return 700;
                    case "v140_xp": return 800;
                    case "v141": return 900;
                    case "v141_xp": return 1000;
                    default: return UnknownToolset;
                }
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