using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.TemplateWizard;
using NewProjectWizard.GTA.Helpers;

namespace NewProjectWizard.GTA
{
    // ReSharper disable once UnusedMember.Global
    public class GtestDllProjectWizard : ProjectWizardBase
    {
        private const string ConfigurationTypePlaceholder = "$gta_configuration_type$";
        private const string CreatedSharedLibraryPlaceholder = "$gta_create_shared_library$";

        protected override void RunStarted()
        {
            var configurationType = ProjectExtensions.ConfigurationType.Static;
            using (var dialog = new CreateGtestProjectDialog()
            {
                ConfigurationType = configurationType
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    throw new WizardCancelledException();
                }

                configurationType = dialog.ConfigurationType;
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
    }
}