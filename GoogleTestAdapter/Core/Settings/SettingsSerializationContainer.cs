using System.Collections.Generic;
using System.Xml.Serialization;

namespace GoogleTestAdapter.Settings
{

    [XmlRoot(GoogleTestConstants.SettingsName)]
    public class SettingsSerializationContainer
    {
        public SingleSettings SolutionSettings { get; set; } = new SingleSettings();

        [XmlArray("ProjectSettings")]
        [XmlArrayItem("Settings")]
        public SettingsList SettingsList { get; set; } = new SettingsList();

        public SettingsSerializationContainer() { }

        public SettingsSerializationContainer(IGoogleTestAdapterSettingsContainer container)
        {
            SolutionSettings.Settings = container.SolutionSettings;
            SettingsList.AddRange(container.ProjectSettings);
        }

    }

    public class SettingsList : List<RunSettings> { }

    public class SingleSettings
    {
        public RunSettings Settings { get; set; }
    }

}