using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    public interface IGlobalRunSettingsInternal2 : IGlobalRunSettings2
    {
        new RunSettings RunSettings { get; set; }
    }

}