using System;
using System.Reflection;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{

    internal class VersionProvider
    {
        private const string CollectionName = "GoogleTestAdapter";
        private const string VersionPropertyName = "LastStartedVersion";

        private WritableSettingsStore SettingsStore { get; }

        internal VersionProvider(IServiceProvider serviceProvider)
        {
            var settingsManager = new ShellSettingsManager(serviceProvider);
            SettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!SettingsStore.CollectionExists(CollectionName))
                SettingsStore.CreateCollection(CollectionName);
        }

        internal Version FormerlyInstalledVersion
        {
            get
            {
                Version formerlyInstalledVersion = null;
                if (SettingsStore.PropertyExists(CollectionName, VersionPropertyName))
                {
                    string versionString = SettingsStore.GetString(CollectionName, VersionPropertyName);
                    formerlyInstalledVersion = Version.Parse(versionString);
                }
                return formerlyInstalledVersion;
            }
        }

        internal Version CurrentVersion
        {
            get
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return new Version(currentVersion.Major, currentVersion.Minor, 0, currentVersion.Revision);
            }
        }

        internal void UpdateLastVersion()
        {
            SettingsStore.SetString(CollectionName, VersionPropertyName, CurrentVersion.ToString());
        }

    }

}