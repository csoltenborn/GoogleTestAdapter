using System;
using EnvDTE;

namespace GoogleTestAdapter.VsPackage.GTA.Settings
{
    public class ProjectProperty
    {
        public string Placeholder { get; }

        private string PropertyPage { get; }
        private string PropertyName { get; }

        public ProjectProperty(string propertyPage, string propertyName, string placeholder)
        {
            PropertyPage = propertyPage;
            PropertyName = propertyName;
            Placeholder = $"${{{placeholder}}}";
        }        

        public ProjectProperty(string propertyPage, string propertyName)
            : this(propertyPage, propertyName, propertyName) { }        

        public string GetValue(Project project)
        {
            try
            {
                string configurationName = project.ConfigurationManager.ActiveConfiguration.ConfigurationName; // "Debug" or "Release"
                string platformName = project.ConfigurationManager.ActiveConfiguration.PlatformName; // "Win32" or "x64"
                string pattern = configurationName + '|' + platformName;

                dynamic vcProject = project.Object; // Microsoft.VisualStudio.VCProjectEngine.VCProject
                dynamic vcConfiguration = vcProject.Configurations.Item(pattern); // Microsoft.VisualStudio.VCProjectEngine.VCConfiguration

                object value = vcConfiguration.Rules.Item(PropertyPage).GetEvaluatedPropertyValue(PropertyName);
                return value?.ToString() ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}