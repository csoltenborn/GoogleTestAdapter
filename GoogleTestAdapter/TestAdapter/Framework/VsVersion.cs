using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.TestAdapter.Helpers;
using Process = System.Diagnostics.Process;

namespace GoogleTestAdapter.TestAdapter.Framework
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VsVersion
    {
        Unknown = -1, VS2012 = 0, VS2012_1 = 11, VS2013 = 12, VS2015 = 14, VS2017 = 15
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
                default:
                    throw new InvalidOperationException();
            }
        }

        public static VsVersion? FromYear(int year)
        {
            switch (year)
            {
                case 2012:
                    return VsVersion.VS2012_1;
                case 2013:
                    return VsVersion.VS2013;
                case 2015:
                    return VsVersion.VS2015;
                case 2017:
                    return VsVersion.VS2017;
                default:
                    return null;
            }
        }

        public static VsVersion? FromVersionNumber(int versionNumber)
        {
            switch (versionNumber)
            {
                case 11:
                    return VsVersion.VS2012_1;
                case 12:
                    return VsVersion.VS2013;
                case 14:
                    return VsVersion.VS2015;
                case 15:
                    return VsVersion.VS2017;
                default:
                    return null;
            }
        }

        public static VsVersion? TryParseVersion(string versionText)
        {
            VsVersion version;
            if (Enum.TryParse(versionText, true, out version))
                return version;

            int number;
            if (int.TryParse(versionText, out number))
                return FromYear(number) ?? FromVersionNumber(number);

            return null;
        }
    }

    public static class VsVersionUtils
    {
        private static readonly Version LastUnsupportedVersion = new Version(11, 0, 50727, 1); // VS2012 without updates

        public static readonly VsVersion FirstSupportedVersion = VsVersion.VS2012_1;
        public static readonly VsVersion LastSupportedVersion = Enum.GetValues(typeof(VsVersion)).Cast<VsVersion>().Max();

        private static readonly object Lock = new object(); 

        private static VsVersion? _version;

        public static VsVersion GetVisualStudioVersion(ILogger logger)
        {
            lock (Lock)
            {
                if (_version.HasValue)
                    return _version.Value;

                try
                {
                    _version = TryGetVsVersionFromEnvironment(logger);

                    if (_version.HasValue)
                        return _version.Value;

                    _version = GetVsVersionFromProcess();
                }
                catch (Exception e)
                {
                    logger?.LogError($"Could not find out VisualStudio version: {e.Message}");
                    _version = VsVersion.Unknown;
                }
                return _version.Value;
            }
        }

        private static VsVersion? TryGetVsVersionFromEnvironment(ILogger logger)
        {
            const string VsVersionEnvironmentVariable = "GOOGLETESTADAPTER_VSVERSION";

            try
            {
                string versionText = Environment.GetEnvironmentVariable(VsVersionEnvironmentVariable);

                if (!string.IsNullOrWhiteSpace(versionText))
                    return VsVersionExtensions.TryParseVersion(versionText);
            }
            catch (System.Security.SecurityException)
            {
                //ignore security exceptions
            }
            catch (Exception e)
            {
                logger?.LogError($"Unexpected error during environment check for VsVersion: {e.Message}");
            }

            //either no environment variable or exception occured
            return null;
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
            while (executable != null && executable != "devenv.exe" && executable != "vstest.console.exe")
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