using GoogleTestAdapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapterVSIX
{

    internal interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

}