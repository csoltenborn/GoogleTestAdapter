using System;
using System.Reflection;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace GoogleTestAdapter.VsPackage.GTA.Shared.ReleaseNotes
{

    public class VersionProvider
    {
        private const string CollectionName = "GoogleTestAdapter";
        private const string OldVersionPropertyName = "LastStartedVersion"; // TODO remove for release 1.0
        private const string VersionPropertyName = "LastVersion";

        private readonly WritableSettingsStore _settingsStore;

        public VersionProvider(IServiceProvider serviceProvider)
        {
            var settingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!_settingsStore.CollectionExists(CollectionName))
                _settingsStore.CreateCollection(CollectionName);

            if (_settingsStore.PropertyExists(CollectionName, OldVersionPropertyName))
                _settingsStore.DeleteProperty(CollectionName, OldVersionPropertyName);
        }

        public Version FormerlyInstalledVersion
        {
            get
            {
                if (!_settingsStore.PropertyExists(CollectionName, VersionPropertyName))
                    return null;

                string versionString = _settingsStore.GetString(CollectionName, VersionPropertyName);
                return Version.Parse(versionString);
            }
        }

        public Version CurrentVersion
        {
            get
            {
                Version currentVersion = Assembly.GetAssembly(typeof(History)).GetName().Version;
                return new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);
            }
        }

        public void UpdateLastVersion()
        {
            _settingsStore.SetString(CollectionName, VersionPropertyName, CurrentVersion.ToString());
        }

    }

}