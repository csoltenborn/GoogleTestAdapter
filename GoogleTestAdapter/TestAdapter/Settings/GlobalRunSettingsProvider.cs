using System.ComponentModel.Composition;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [Export(typeof(IGlobalRunSettings))]
    [Export(typeof(IGlobalRunSettingsInternal))]
    public class GlobalRunSettingsProvider : IGlobalRunSettingsInternal
    {
        public RunSettings RunSettings { get; set; } = new RunSettings();
    }

}