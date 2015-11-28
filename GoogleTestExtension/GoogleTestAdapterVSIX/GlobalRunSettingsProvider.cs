using System.ComponentModel.Composition;
using GoogleTestAdapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapterVSIX
{

    [Export(typeof(IGlobalRunSettings))]
    [Export(typeof(IGlobalRunSettingsInternal))]
    public class GlobalRunSettingsProvider : IGlobalRunSettingsInternal
    {
        public RunSettings RunSettings { get; set; } = new RunSettings();
    }

}