using System.Collections.Generic;
using System.Linq;
using System.Xml;
using EnvDTE;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    public abstract class ProjectWizardBase : IWizard
    {
        protected const string ToolsetPlaceholder = "$gta_toolset$";
        private const string GenerateDebugInformationPlaceholder = "$gta_generate_debug_information$";
        private const string VariadicMaxPlaceholder = "$gta_variadic_max$";

        protected ILogger Logger { get; }
        protected SettingsWrapper Settings { get; }

        protected IList<Project> CppProjects { get; private set; }
        protected IDictionary<string, string> ReplacementsDictionary { get; private set; }

        protected ProjectWizardBase()
        {
            var componentModel = (IComponentModel) Package.GetGlobalService(typeof(SComponentModel));
            var runSettings = componentModel.GetService<IGlobalRunSettingsInternal>();
            Settings = new SettingsWrapper(new RunSettingsContainer(runSettings.RunSettings));
            Logger = new TestWindowLogger(() => Settings.OutputMode, () => Settings.TimestampMode);
            Settings.RegexTraitParser = new RegexTraitParser(Logger);
            Settings.HelperFilesCache = new HelperFilesCache(Logger);

            Logger.DebugInfo($"VS version: '{VisualStudioHelper.GetVisualStudioVersionString()}'");
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            CppProjects = ((_DTE) automationObject).Solution.Projects
                .Cast<Project>()
                .Where(p => p.IsCppProject())
                .ToList();
            ReplacementsDictionary = replacementsDictionary;

            FillReplacementsDictionary();
            RunStarted();
        }

        private void FillReplacementsDictionary()
        {
            string value = VisualStudioHelper.GetGenerateDebugInformationFromVisualStudioVersion();
            ReplacementsDictionary.Add(GenerateDebugInformationPlaceholder, value);
            Logger.DebugInfo($"GenerateDebugInfo: '{value}'");

            value = VisualStudioHelper.GetVariadicMaxFromVisualStudioVersion();
            ReplacementsDictionary.Add(VariadicMaxPlaceholder, value);
            Logger.DebugInfo($"VariadixMax: '{value}'");
        }

        protected abstract void RunStarted();

        protected string GetPlatformToolset(ICollection<Project> projects)
        {
            try
            {
                var platformToolset = GetPlatformToolsetFromProjects(projects);
                if (platformToolset != null)
                {
                    return platformToolset;
                }
            }
            catch
            {
                // fallback to toolset by VS version
            }

            string result = VisualStudioHelper.GetPlatformToolsetFromVisualStudioVersion();
            Logger.DebugInfo($"Toolset from VS version: '{result}'");
            return result;
        }

        private string GetPlatformToolsetFromProjects(ICollection<Project> projects)
        {
            var toolsetsInUse = new HashSet<string>();
            foreach (Project project in projects)
            {
                XmlDocument projectFile = new XmlDocument();
                projectFile.Load(project.FullName);
                var nodes = projectFile.GetElementsByTagName("PlatformToolset");
                foreach (XmlNode node in nodes)
                {
                    toolsetsInUse.Add(node.InnerText);
                }
            }

            var comparer = new ToolsetComparer();
            var orderedToolsetsInUse = toolsetsInUse.OrderByDescending(ts => ts, comparer).ToList();

            Logger.DebugInfo($"Projects considered when searching Toolsets: {string.Join(", ", projects.Select(p => p.Name))}");
            Logger.DebugInfo($"Toolsets found (known to GTA): {string.Join(", ", orderedToolsetsInUse.Select(ts => $"{ts}({comparer.IsKnownToolset(ts)})"))}");

            var result = orderedToolsetsInUse.FirstOrDefault(comparer.IsKnownToolset);
            Logger.DebugInfo(result != null 
                ? $"Toolset selected: '{result}'" 
                : "No toolset found in projects");
            return result;
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
                    case "v142": return 1100;
                    case "v142_xp": return 1200;
                    default: return UnknownToolset;
                }
            }
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