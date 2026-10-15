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
        Unknown = -1, VS2012 = 0, VS2012_1 = 11, VS2013 = 12, VS2015 = 14, VS2017 = 15, VS2019 = 16, VS2022 = 17, VS2026 = 18
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
                case VsVersion.VS2022:
                    return 2022;
                case VsVersion.VS2026:
                    return 2026;
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

        // GTA requires VS 2017 or later, so an unknown version is not assumed to need throttling
        public static bool NeedsToBeThrottled(this VsVersion version)
        {
            switch (version)
            {
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


        // the process tree of a test host is not expected to be deeper, the limit protects against cycles (process ids
        // of exited parents might have been reused)
        private const int MaxNrOfParentProcesses = 32;

        public static readonly VsVersion VsVersion;

        /// <summary>
        /// The reason why <see cref="VsVersion"/> is <see cref="TestAdapter.Framework.VsVersion.Unknown"/>, null otherwise
        /// </summary>
        public static readonly string VsVersionDetectionError;

        static VsVersionUtils()
        {
            VsVersion = GetVisualStudioVersion(out VsVersionDetectionError);
        }

        private static VsVersion GetVisualStudioVersion(out string error)
        {
            error = null;
            try
            {
                return GetVsVersionFromProcess();
            }
            catch (Exception e)
            {
                error = e.Message;
                return VsVersion.Unknown;
            }
        }

        // after http://stackoverflow.com/questions/11082436/detect-the-visual-studio-version-inside-a-vspackage
        private static VsVersion GetVsVersionFromProcess()
        {
            string pathToBinary = FindVsOrVsTestConsoleExe();
            if (pathToBinary == null)
                throw new InvalidOperationException("Could not find devenv.exe or vstest.*.exe among the parent processes");

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

        /// <returns>The path of the first process up the process tree which is VS or a VSTest host, or null</returns>
        public static string FindVsOrVsTestConsoleExe()
        {
            int processId = Process.GetCurrentProcess().Id;
            for (int i = 0; i < MaxNrOfParentProcesses && processId > 0; i++)
            {
                string path = ParentProcessUtils.GetExecutablePath(processId);
                if (path == null)
                    return null;
                if (ParentProcessRegex.IsMatch(Path.GetFileName(path).Trim()))
                    return path;

                processId = ParentProcessUtils.GetParentProcessId(processId);
            }
            return null;
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