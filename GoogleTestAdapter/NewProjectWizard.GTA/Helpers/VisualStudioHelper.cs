using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace NewProjectWizard.GTA.Helpers
{
    public static class VisualStudioHelper
    {
        public static string GetVisualStudioVersionString()
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            return dte.Version;
        }

        public static string GetPlatformToolsetFromVisualStudioVersion()
        {
            string version = GetVisualStudioVersionString();
            switch (version)
            {
                case "10.0": return "v100";
                case "11.0": return "v110";
                case "12.0": return "v120";
                case "14.0": return "v140";
                case "15.0": return "v141";
                case "16.0": return "v142";
                default: throw new InvalidOperationException($"'{version}' is not a valid version for GTA");
            }
        }

        public static string GetGenerateDebugInformationFromVisualStudioVersion()
        {
            string version = GetVisualStudioVersionString();
            switch (version)
            {
                case "10.0":
                case "11.0":
                case "12.0":
                case "14.0":
                    return "true";
                case "15.0":
                case "16.0":
                    return "DebugFull";
                default: 
                    throw new InvalidOperationException($"'{version}' is not a valid version for GTA");
            }
        }

        public static string GetVariadicMaxFromVisualStudioVersion()
        {
            string version = GetVisualStudioVersionString();
            switch (version)
            {
                case "10.0":
                case "11.0":
                case "12.0":
                    return "_VARIADIC_MAX=10;";
                case "14.0":
                case "15.0":
                case "16.0":
                    return "";
                default:
                    throw new InvalidOperationException($"'{version}' is not a valid version for GTA");
            }
        }
    }
}