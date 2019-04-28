using System;
using System.Reflection;
using GoogleTestAdapter.VsPackage.GTA.Helpers;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{

    internal class VersionProvider
    {
        private const string VersionPropertyName = "LastVersion";

        internal Version FormerlyInstalledVersion
        {
            get
            {
                if (!VsSettingsStorage.Instance.PropertyExists(VersionPropertyName))
                    return null;

                string versionString = VsSettingsStorage.Instance.GetString(VersionPropertyName);
                return Version.Parse(versionString);
            }
        }

        internal Version CurrentVersion
        {
            get
            {
                Version currentVersion = Assembly.GetAssembly(typeof(History)).GetName().Version;
                return new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);
            }
        }

        internal void UpdateLastVersion()
        {
            VsSettingsStorage.Instance.SetString(VersionPropertyName, CurrentVersion.ToString());
        }

    }

}