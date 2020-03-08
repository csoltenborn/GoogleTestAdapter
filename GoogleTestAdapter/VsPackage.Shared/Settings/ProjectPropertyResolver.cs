using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using GoogleTestAdapter.TestAdapter.VisualStudio;
using Microsoft.VisualStudio.Shell;
// ReSharper disable InconsistentNaming

namespace GoogleTestAdapter.VsPackage.GTA.Settings
{
    public class ProjectPropertyResolver
    {
        private const string PropertyPage_Debugger = "WindowsLocalDebugger";
        private const string PropertyPage_General = "ConfigurationGeneral";
        private const string PropertyPage_Directories = "ConfigurationDirectories";

        // from C:\Program Files (x86)\MSBuild\Microsoft.Cpp\v4.0\V120\1033\*.xml
        private static readonly IReadOnlyCollection<ProjectProperty> _projectProperties = new List<ProjectProperty>
        {
            new ProjectProperty(PropertyPage_General, "OutDir"),
            new ProjectProperty(PropertyPage_General, "TargetName"),
            new ProjectProperty(PropertyPage_General, "TargetExt"),
            new ProjectProperty(PropertyPage_Debugger, "LocalDebuggerCommandArguments", "CommandArguments"),
            new ProjectProperty(PropertyPage_Debugger, "LocalDebuggerWorkingDirectory", "VS_WorkingDirectory"),
            new ProjectProperty(PropertyPage_Debugger, "LocalDebuggerEnvironment", "Environment"),
            new ProjectProperty(PropertyPage_General, "PlatformToolset"),
            new ProjectProperty(PropertyPage_Directories, "IncludePath"),
            new ProjectProperty(PropertyPage_Directories, "AdditionalIncludeDirectories")
        };

        private bool ProducesExecutable(Project project, string executable)
        {
            string configurationName = project.ConfigurationManager.ActiveConfiguration.ConfigurationName; // "Debug" or "Release"
            string platformName = project.ConfigurationManager.ActiveConfiguration.PlatformName; // "Win32" or "x64"
            string pattern = $"{configurationName}|{platformName}";

            dynamic vcProject = project.Object; // Microsoft.VisualStudio.VCProjectEngine.VCProject
            dynamic vcConfiguration = vcProject.Configurations.Item(pattern); // Microsoft.VisualStudio.VCProjectEngine.VCConfiguration
            
            string outDir = vcConfiguration.Rules.Item(PropertyPage_General).GetEvaluatedPropertyValue("OutDir");
            string targetName = vcConfiguration.Rules.Item(PropertyPage_General).GetEvaluatedPropertyValue("TargetName");
            string targetExt = vcConfiguration.Rules.Item(PropertyPage_General).GetEvaluatedPropertyValue("TargetExt");

            string file = outDir + targetName + targetExt;

            var executablePath = Path.GetFullPath(executable);
            var projectFilePath = Path.GetFullPath(file);

            return string.Equals(executablePath, projectFilePath, StringComparison.OrdinalIgnoreCase);
        }

        public IDictionary<string, string> GetPlaceholderDictionary(string executable)
        {
            try
            {
                if (Package.GetGlobalService(typeof(DTE)) is DTE dte)
                {
                    var cppProject = dte.Solution.Projects
                        .Cast<Project>()
                        .FirstOrDefault(p => p.IsCppProject() && ProducesExecutable(p, executable));

                    return GetPlaceholderDictionary(cppProject);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return new Dictionary<string, string>();
        }

        public IDictionary<string, string> GetPlaceholderDictionary(Project project)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var projectProperty in _projectProperties)
            {
                dictionary.Add(projectProperty.Placeholder, projectProperty.GetValue(project));
            }

            return dictionary;
        }
    }

}