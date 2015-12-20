namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Settings
{

    public interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

}