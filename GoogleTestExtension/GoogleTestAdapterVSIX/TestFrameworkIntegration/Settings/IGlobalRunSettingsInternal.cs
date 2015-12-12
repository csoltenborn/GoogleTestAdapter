namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Settings
{

    interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

}