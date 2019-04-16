// This file has been modified by Microsoft on 6/2017.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.ComponentModel.Composition;
using System.Xml;
using System.Xml.XPath;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    [Export(typeof(ISettingsProvider))]
    [SettingsName(GoogleTestConstants.SettingsName)]
    public class RunSettingsProvider : ISettingsProvider
    {
        // virtual for mocking
        public virtual RunSettingsContainer SettingsContainer { get; private set; }

        public string Name { get; private set; } = GoogleTestConstants.SettingsName;

        public void Load(XmlReader reader)
        {
            var document = new XPathDocument(reader);
            var navigator = document.CreateNavigator();
            RunSettingsContainer container = null;
            try
            {
                if (navigator.MoveToChild(GoogleTestConstants.SettingsName, ""))
                    container = RunSettingsContainer.LoadFromXml(navigator);
            }
            catch (InvalidRunSettingsException)
            { }
            SettingsContainer = container ?? new RunSettingsContainer();
        }

    }

}