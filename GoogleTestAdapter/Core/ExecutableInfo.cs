using System.IO;
using System.Security;
using System.Security.Policy;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter
{
    /// <summary>
    /// What GTA's test discovery decides about an executable, and why (e.g. for GTA's automation object). The executable
    /// is not run.
    /// </summary>
    public class ExecutableInfo
    {
        public string Executable { get; private set; }
        public bool Exists { get; private set; }
        // GTA's discoverer handles .exe files only
        public bool IsExe { get; private set; }

        public SecurityZone SecurityZone { get; private set; }
        public bool IsOnNetworkDrive { get; private set; }
        public bool SkipOriginCheck { get; private set; }
        public bool IsTrusted { get; private set; }

        public string TestDiscoveryRegex { get; private set; }
        /// <summary>One of the GoogleTestDiscoverer.Indicator* constants, null if not a Google Test executable</summary>
        public string GoogleTestIndicator { get; private set; }

        public string Pdb { get; private set; }

        public bool WouldBeDiscovered => Exists && IsExe && IsTrusted && GoogleTestIndicator != null;

        /// <param name="settings">the settings for the executable (see SettingsWrapper.ExecuteWithSettingsForExecutable())</param>
        public static ExecutableInfo Create(string executable, SettingsWrapper settings, ILogger logger)
        {
            executable = Path.GetFullPath(executable);
            var info = new ExecutableInfo
            {
                Executable = executable,
                Exists = File.Exists(executable),
                IsExe = string.Equals(Path.GetExtension(executable), ".exe", System.StringComparison.OrdinalIgnoreCase),
                SecurityZone = Zone.CreateFromUrl(executable).SecurityZone,
                IsOnNetworkDrive = GoogleTestDiscoverer.IsOnNetworkDrive(executable),
                SkipOriginCheck = settings.SkipOriginCheck,
                TestDiscoveryRegex = settings.TestDiscoveryRegex
            };
            info.IsTrusted = info.SkipOriginCheck || GoogleTestDiscoverer.IsTrustedZone(info.SecurityZone);

            if (info.Exists)
            {
                info.GoogleTestIndicator = GoogleTestDiscoverer.GetGoogleTestExecutableIndicator(executable, settings.TestDiscoveryRegex, logger);
                info.Pdb = PdbLocator.FindPdbFile(executable, settings.GetPathExtension(executable), logger);
            }
            return info;
        }
    }
}
