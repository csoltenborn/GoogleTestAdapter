using System;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    // ReSharper disable once UnusedMember.Global
    public class GtestDllProjectWizard : ProjectWizardBase
    {
        private const string ConfigurationTypePlaceholder = "$gta_configuration_type$";
        private const string CreatedSharedLibraryPlaceholder = "$gta_create_shared_library$";
        private const string TargetExtensionPlaceholder = "$gta_target_extension$";

        private bool _includeGoogleMock = true;

        protected override void RunStarted()
        {
            var configurationType = ProjectExtensions.ConfigurationType.Static;
            using (var dialog = new CreateGtestProjectDialog()
            {
                ConfigurationType = configurationType,
                IncludeGoogleMock = _includeGoogleMock
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    throw new WizardCancelledException();
                }

                configurationType = dialog.ConfigurationType;
                _includeGoogleMock = dialog.IncludeGoogleMock;
            }

            string value = GetPlatformToolset(CppProjects);
            ReplacementsDictionary.Add(ToolsetPlaceholder, value);
            Logger.DebugInfo($"Platform toolset: '{value}'");

            value = GetConfigurationType(configurationType);
            ReplacementsDictionary.Add(ConfigurationTypePlaceholder, value);
            Logger.DebugInfo($"Configuration type: '{value}'");

            value = configurationType == ProjectExtensions.ConfigurationType.Dynamic
                ? "GTEST_CREATE_SHARED_LIBRARY;"
                : "";
            ReplacementsDictionary.Add(CreatedSharedLibraryPlaceholder, value);
            Logger.DebugInfo($"Create shared library: '{value}'");

            value = configurationType == ProjectExtensions.ConfigurationType.Dynamic
                ? ".dll"
                : ".lib";
            ReplacementsDictionary.Add(TargetExtensionPlaceholder, value);
            Logger.DebugInfo($"Target extension: '{value}'");
        }

        public override void ProjectFinishedGenerating(Project project)
        {
            base.ProjectFinishedGenerating(project);

            if (_includeGoogleMock)
            {
                DeleteProjectItem(project.ProjectItems, "gtest-all.cc");
            }
            else
            {
                DeleteProjectItem(project.ProjectItems, "gmock-gtest-all.cc");
                DeleteProjectItem(project.ProjectItems, "gmock.h");

                try
                {
                    string projectDir = Path.GetDirectoryName(project.FullName);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    string gmockDir = Path.Combine(projectDir, "include", "gmock");
                    Directory.Delete(gmockDir);
                }
                catch (Exception)
                {
                    // too bad
                }
            }
        }

        private string GetConfigurationType(ProjectExtensions.ConfigurationType configurationType)
        {
            switch (configurationType)
            {
                case ProjectExtensions.ConfigurationType.Static:
                    return ProjectExtensions.ConfigurationTypeStatic;
                case ProjectExtensions.ConfigurationType.Dynamic:
                    return ProjectExtensions.ConfigurationTypeDynamic;
                default:
                    throw new InvalidOperationException($"Unkown literal: {configurationType}");
            }
        }

        private void DeleteProjectItem(ProjectItems projectItems, string fileName)
        {
            try
            {
                var projectItem = FindProjectItem(projectItems, fileName);
                if (projectItem == null)
                {
                    Logger.LogWarning($"Could not find and thus delete file {fileName} - problems may occur. Try to delete file manually.");
                }
                else
                {
                    projectItem.Delete();
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Could not delete file {fileName} since an exception occured (message: '{e.Message}') - problems may occur. Try to delete file manually.");
                Logger.DebugWarning($"Exception:{Environment.NewLine}{e}");
            }
        }

        private ProjectItem FindProjectItem(ProjectItems projectItems, string name)
        {
            foreach (ProjectItem projectItem in projectItems)
            {
                if (name == projectItem.Name)
                    return projectItem;

                var subProjectItem = FindProjectItem(projectItem.ProjectItems, name);
                if (subProjectItem != null)
                    return subProjectItem;
            }

            return null;
        }

    }
}