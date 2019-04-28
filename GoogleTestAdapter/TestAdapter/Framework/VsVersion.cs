using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.TestAdapter.Helpers;
using Process = System.Diagnostics.Process;

namespace GoogleTestAdapter.TestAdapter.Framework
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VsVersion
    {
        Unknown = -1, VS2012 = 0, VS2012_1 = 11, VS2013 = 12, VS2015 = 14, VS2017 = 15, VS2019 = 16
    }

    public static class VsVersionExtensions
    {
        public static int Year(this VsVersion version)
        {
            switch (version)
            {
                case VsVersion.Unknown:
                    return 0;
                case VsVersion.VS2012:
                case VsVersion.VS2012_1:
                    return 2012;
                case VsVersion.VS2013:
                    return 2013;
                case VsVersion.VS2015:
                    return 2015;
                case VsVersion.VS2017:
                    return 2017;
                case VsVersion.VS2019:
                    return 2019;
                default:
                    throw new InvalidOperationException();
            }

        }

        public static string VersionString(this VsVersion version)
        {
            switch (version)
            {
                case VsVersion.Unknown:
                case VsVersion.VS2012:
                    return "0.0";
                default:
                    return $"{(int)version}.0";
            }
        }

        public static bool NeedsToBeThrottled(this VsVersion version)
        {
            switch (version)
            {
                case VsVersion.Unknown:
                case VsVersion.VS2012:
                case VsVersion.VS2012_1:
                case VsVersion.VS2013:
                case VsVersion.VS2015:
                    return true;
                default:
                    return false;
            }
        }

        public static bool PrintsTimeStampAndSeverity(this VsVersion version)
        {
            switch (version)
            {
                case VsVersion.Unknown:
                case VsVersion.VS2012:
                case VsVersion.VS2012_1:
                case VsVersion.VS2013:
                case VsVersion.VS2015:
                    return false;
                default:
                    return true;
            }
        }
    }

    public static class VsVersionUtils
    {
        private const string ParentProcessPattern = @"(^vstest\.((discoveryengine)|(executionengine)|(console)).*\.exe$)|(^devenv\.exe$)";
        private static readonly Regex ParentProcessRegex = new Regex(ParentProcessPattern, RegexOptions.IgnoreCase);

        private static readonly Version LastUnsupportedVersion = new Version(11, 0, 50727, 1); // VS2012 without updates

        public static readonly VsVersion FirstSupportedVersion = VsVersion.VS2012_1;
        public static readonly VsVersion LastSupportedVersion = Enum.GetValues(typeof(VsVersion)).Cast<VsVersion>().Max();


        public static readonly VsVersion VsVersion;

        static VsVersionUtils()
        {
            VsVersion = GetVisualStudioVersion();
        }

        private static VsVersion GetVisualStudioVersion()
        {
            try
            {
                return GetVsVersionFromProcess();
            }
            catch (Exception)
            {
                return VsVersion.Unknown;
            }
        }

        // after http://stackoverflow.com/questions/11082436/detect-the-visual-studio-version-inside-a-vspackage
        private static VsVersion GetVsVersionFromProcess()
        {
            string pathToBinary = FindVsOrVsTestConsoleExe()?.MainModule.FileName;
            if (pathToBinary == null)
                throw new InvalidOperationException("Could not find process");

            FileVersionInfo binaryVersionInfo = FileVersionInfo.GetVersionInfo(pathToBinary);

            string versionString = binaryVersionInfo.ProductVersion;
            for (int i = 0; i < versionString.Length; i++)
            {
                if (!char.IsDigit(versionString, i) && versionString[i] != '.')
                {
                    versionString = versionString.Substring(0, i);
                    break;
                }
            }

            return GetVsVersionFromVersionString(versionString);
        }

        private static Process FindVsOrVsTestConsoleExe()
        {
            var process = Process.GetCurrentProcess();
            string executable = Path.GetFileName(process.MainModule.FileName).Trim().ToLower();
            while (executable != null && !ParentProcessRegex.IsMatch(executable))
            {
                process = ParentProcessUtils.GetParentProcess(process.Id);
                executable = process != null 
                    ? Path.GetFileName(process.MainModule.FileName).Trim().ToLower()
                    : null;
            }
            return process;
        }

        private static VsVersion GetVsVersionFromVersionString(string versionString)
        {
            var version = Version.Parse(versionString);

            if (version <= LastUnsupportedVersion)
                return VsVersion.VS2012;

            if (version.Major < (int) FirstSupportedVersion || version.Major > (int) LastSupportedVersion)
                throw new InvalidOperationException($"Unknown VisualStudio version: {versionString}");

            return (VsVersion) version.Major;
        }
    }

}