namespace GoogleTestAdapter.TestAdapter.Settings
{

    public interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

}