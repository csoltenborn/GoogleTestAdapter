using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    /// <summary>
    /// The solution directory and the active configuration of Visual Studio, which provide the values of the
    /// placeholders $(SolutionDir), $(PlatformName), and $(ConfigurationName).
    /// </summary>
    public class VisualStudioConfiguration
    {
        /// <summary>
        /// The full name of the solution file, or the opened folder if VS is in Open Folder mode
        /// </summary>
        public string SolutionFullName { get; set; }
        public string SolutionDir { get; set; }
        public string PlatformName { get; set; }
        public string ConfigurationName { get; set; }

        // DTE is not used since its interop types are incompatible with the managed DTE implementation of VS 2026;
        // must be called on the UI thread
        public static VisualStudioConfiguration FromServiceProvider(IServiceProvider serviceProvider, Action<string> logError)
        {
            var configuration = new VisualStudioConfiguration();
            var solution = serviceProvider?.GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution == null)
                return configuration;

            try
            {
                string solutionFullName = GetSolutionFullName(solution);
                configuration.SolutionFullName = solutionFullName;
                try
                {
                    configuration.SolutionDir = SolutionPaths.GetSolutionDir(solutionFullName);
                }
                catch (Exception e)
                {
                    logError($"Exception caught while receiving solution dir from VS instance. Solution full name: {solutionFullName}. Exception:{Environment.NewLine}{e}");
                }

                var buildManager = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
                var project = GetFirstProject(solution);
                var projectConfigurations = new IVsProjectCfg[1];
                if (buildManager != null && project != null
                    && ErrorHandler.Succeeded(buildManager.FindActiveProjectCfg(IntPtr.Zero, IntPtr.Zero, project, projectConfigurations))
                    && projectConfigurations[0] != null
                    && ErrorHandler.Succeeded(projectConfigurations[0].get_CanonicalName(out string canonicalName)))
                {
                    // canonical names have the format "ConfigurationName|PlatformName"
                    string[] parts = canonicalName.Split('|');
                    configuration.ConfigurationName = parts[0];
                    configuration.PlatformName = parts.Length > 1 ? parts[1] : null;
                }
            }
            catch (Exception e)
            {
                logError($"Exception while receiving configuration info from Visual Studio.{Environment.NewLine}{e}");
            }

            return configuration;
        }

        private static string GetSolutionFullName(IVsSolution solution)
        {
            string solutionFileName = GetStringProperty(solution, __VSPROPID.VSPROPID_SolutionFileName);
            return string.IsNullOrEmpty(solutionFileName)
                ? GetStringProperty(solution, __VSPROPID.VSPROPID_SolutionDirectory)
                : solutionFileName;
        }

        private static string GetStringProperty(IVsSolution solution, __VSPROPID property)
        {
            return ErrorHandler.Succeeded(solution.GetProperty((int)property, out object value))
                ? value as string
                : null;
        }

        private static IVsHierarchy GetFirstProject(IVsSolution solution)
        {
            Guid ignored = Guid.Empty;
            if (ErrorHandler.Failed(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref ignored, out IEnumHierarchies projects)))
                return null;

            var hierarchies = new IVsHierarchy[1];
            return projects.Next(1, hierarchies, out uint fetched) == VSConstants.S_OK && fetched == 1
                ? hierarchies[0]
                : null;
        }
    }
}
