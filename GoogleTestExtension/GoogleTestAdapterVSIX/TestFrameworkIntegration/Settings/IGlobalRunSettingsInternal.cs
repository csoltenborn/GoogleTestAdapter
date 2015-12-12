namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Settings
{

    internal interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

}