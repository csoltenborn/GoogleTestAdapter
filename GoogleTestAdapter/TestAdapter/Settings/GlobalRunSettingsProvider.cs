using System.ComponentModel.Composition;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [Export(typeof(IGlobalRunSettings2))]
    [Export(typeof(IGlobalRunSettingsInternal2))]
    public class GlobalRunSettingsProvider : IGlobalRunSettingsInternal2
    {
        public RunSettings RunSettings { get; set; } = new RunSettings();
    }

}