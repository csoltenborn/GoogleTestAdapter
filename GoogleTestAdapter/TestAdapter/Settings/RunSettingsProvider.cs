using System.ComponentModel.Composition;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    [Export(typeof(ISettingsProvider))]
    [SettingsName(GoogleTestConstants.SettingsName)]
    public class RunSettingsProvider : ISettingsProvider
    {
        public RunSettings Settings { get; private set; }

        public string Name { get; private set; } = GoogleTestConstants.SettingsName;

        public void Load(XmlReader reader)
        {
            Settings = RunSettings.LoadFromXml(reader);
        }
    }

}
