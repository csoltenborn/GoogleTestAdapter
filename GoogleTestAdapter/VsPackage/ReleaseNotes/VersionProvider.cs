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

        private readonly WritableSettingsStore _settingsStore;

        internal VersionProvider(IServiceProvider serviceProvider)
        {
            var settingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!_settingsStore.CollectionExists(CollectionName))
                _settingsStore.CreateCollection(CollectionName);
        }

        internal Version FormerlyInstalledVersion
        {
            get
            {
                Version formerlyInstalledVersion = null;
                if (_settingsStore.PropertyExists(CollectionName, VersionPropertyName))
                {
                    string versionString = _settingsStore.GetString(CollectionName, VersionPropertyName);
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
            _settingsStore.SetString(CollectionName, VersionPropertyName, CurrentVersion.ToString());
        }

    }

}