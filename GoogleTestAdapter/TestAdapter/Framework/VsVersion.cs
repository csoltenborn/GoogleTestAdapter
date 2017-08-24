// This file has been modified by Microsoft on 8/2017.

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
                    _version = GetVsVersionFromProcess();
                }
                catch (Exception e)
                {
                    logger?.LogError(String.Format(Resources.VSVersionMessage, e.Message));
                    _version = VsVersion.Unknown;
                }
                return _version.Value;
            }
        }

        // after http://stackoverflow.com/questions/11082436/detect-the-visual-studio-version-inside-a-vspackage
        private static VsVersion GetVsVersionFromProcess()
        {
            string pathToBinary = FindVsOrVsTestConsoleExe()?.MainModule.FileName;
            if (pathToBinary == null)
                throw new InvalidOperationException(Resources.ProcessNotFound);

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
                throw new InvalidOperationException(String.Format(Resources.UnknownVisualStudioVersion, versionString));

            return (VsVersion) version.Major;
        }
    }

}