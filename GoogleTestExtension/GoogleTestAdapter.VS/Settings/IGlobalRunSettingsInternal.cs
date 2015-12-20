namespace GoogleTestAdapter.VS.Settings
{

    public interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

}