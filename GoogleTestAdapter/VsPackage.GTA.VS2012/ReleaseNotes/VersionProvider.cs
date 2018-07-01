using System;
using System.Reflection;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{

    internal class VersionProvider
    {
        private const string CollectionName = "GoogleTestAdapter";
        private const string OldVersionPropertyName = "LastStartedVersion"; // TODO remove for release 1.0
        private const string VersionPropertyName = "LastVersion";

        private readonly WritableSettingsStore _settingsStore;

        internal VersionProvider(IServiceProvider serviceProvider)
        {
            var settingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!_settingsStore.CollectionExists(CollectionName))
                _settingsStore.CreateCollection(CollectionName);

            if (_settingsStore.PropertyExists(CollectionName, OldVersionPropertyName))
                _settingsStore.DeleteProperty(CollectionName, OldVersionPropertyName);
        }

        internal Version FormerlyInstalledVersion
        {
            get
            {
                if (!_settingsStore.PropertyExists(CollectionName, VersionPropertyName))
                    return null;

                string versionString = _settingsStore.GetString(CollectionName, VersionPropertyName);
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
            _settingsStore.SetString(CollectionName, VersionPropertyName, CurrentVersion.ToString());
        }

    }

}