using System.ComponentModel.Composition;

namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Settings
{

    [Export(typeof(IGlobalRunSettings))]
    [Export(typeof(IGlobalRunSettingsInternal))]
    public class GlobalRunSettingsProvider : IGlobalRunSettingsInternal
    {
        public RunSettings RunSettings { get; set; } = new RunSettings();
    }

}