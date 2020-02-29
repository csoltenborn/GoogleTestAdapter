using System;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace GoogleTestAdapter.VsPackage.GTA.Helpers
{
    public class VsSettingsStorage
    {
        private const string CollectionName = "GoogleTestAdapter";

        public static VsSettingsStorage Instance { get; private set; }

        public static void Init(IServiceProvider serviceProvider)
        {
            if (Instance != null)
            {
                throw new Exception();
            }
            Instance = new VsSettingsStorage(serviceProvider);
        }


        private readonly WritableSettingsStore _settingsStore;

        private VsSettingsStorage(IServiceProvider serviceProvider)
        {
            var settingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!_settingsStore.CollectionExists(CollectionName))
                _settingsStore.CreateCollection(CollectionName);
        }

        public bool PropertyExists(string propertyName)
        {
            return _settingsStore.PropertyExists(CollectionName, propertyName);
        }

        public string GetString(string propertyName)
        {
            return _settingsStore.GetString(CollectionName, propertyName);
        }

        public void SetString(string propertyName, string newValue)
        {
            _settingsStore.SetString(CollectionName, propertyName, newValue);
        }

    }
}