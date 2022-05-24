// This file has been modified by Microsoft on 6/2017.

namespace GoogleTestAdapter.Settings
{

    /*
    To add a new option, make the following changes:
    - add (nullable!) property to GoogleTestAdapter.Settings.IGoogleTestAdapterSettings
    - handle property in method GoogleTestAdapter.Settings.GoogleTestAdapterSettingsExtensions.GetUnsetValuesFrom()
    - add property and according constants to class GoogleTestAdapter.Settings.SettingsWrapper
    - handle property serialization in class GoogleTestAdapter.Settings.RunSettings
    - add Options UI integration to one of the classes in GoogleTestAdapter.VsPackage.OptionsPages.*
    - handle property in method GoogleTestAdapter.VsPackage.GoogleTestExtensionOptionsPage.GetRunSettingsFromOptionPages()
    - update schema in GoogleTestAdapterSettings.xsd
    - add new option to Solution Items/AllTestSettings.gta.runsettings
    - add default mock configuration in method GoogleTestAdapter.Tests.Common.TestsBase.SetUp()
    */
    public interface IGoogleTestAdapterSettings
    {
        string ProjectRegex { get; set; }
        string AdditionalTestDiscoveryParam { get; set; }
        string AdditionalTestExecutionParam { get; set; }
        bool? CatchExceptions { get; set; }
        bool? BreakOnFailure { get; set; }
        int? MaxNrOfThreads { get; set; }
        int? NrOfTestRepetitions { get; set; }
        bool? ParallelTestExecution { get; set; }
        bool? PrintTestOutput { get; set; }
        bool? RunDisabledTests { get; set; }
        bool? ShuffleTests { get; set; }
        int? ShuffleTestsSeed { get; set; }
        string TestDiscoveryRegex { get; set; }
        int? TestDiscoveryTimeoutInSeconds { get; set; }
        string WorkingDir { get; set; }
        string PathExtension { get; set; }
        string BatchForTestSetup { get; set; }
        string BatchForTestTeardown { get; set; }
        string TraitsRegexesAfter { get; set; }
        string TraitsRegexesBefore { get; set; }
        string TestNameSeparator { get; set; }
        bool? ParseSymbolInformation { get; set; }
        bool? DebugMode { get; set; }
        bool? TimestampOutput { get; set; }
        bool? ShowReleaseNotes { get; set; }
        bool? KillProcessesOnCancel { get; set; }

        bool? UseNewTestExecutionFramework { get; set; }

        // internal
        string DebuggingNamedPipeId { get; set; }
    }

    public static class GoogleTestAdapterSettingsExtensions
    {
        public static void GetUnsetValuesFrom(this IGoogleTestAdapterSettings self, IGoogleTestAdapterSettings other)
        {
            self.AdditionalTestDiscoveryParam = self.AdditionalTestDiscoveryParam ?? other.AdditionalTestDiscoveryParam;
            self.AdditionalTestExecutionParam = self.AdditionalTestExecutionParam ?? other.AdditionalTestExecutionParam;
            self.CatchExceptions = self.CatchExceptions ?? other.CatchExceptions;
            self.BreakOnFailure = self.BreakOnFailure ?? other.BreakOnFailure;
            self.MaxNrOfThreads = self.MaxNrOfThreads ?? other.MaxNrOfThreads;
            self.NrOfTestRepetitions = self.NrOfTestRepetitions ?? other.NrOfTestRepetitions;
            self.ParallelTestExecution = self.ParallelTestExecution ?? other.ParallelTestExecution;
            self.PrintTestOutput = self.PrintTestOutput ?? other.PrintTestOutput;
            self.RunDisabledTests = self.RunDisabledTests ?? other.RunDisabledTests;
            self.ShuffleTests = self.ShuffleTests ?? other.ShuffleTests;
            self.ShuffleTestsSeed = self.ShuffleTestsSeed ?? other.ShuffleTestsSeed;
            self.TestDiscoveryRegex = self.TestDiscoveryRegex ?? other.TestDiscoveryRegex;
            self.TestDiscoveryTimeoutInSeconds = self.TestDiscoveryTimeoutInSeconds ?? other.TestDiscoveryTimeoutInSeconds;
            self.WorkingDir = self.WorkingDir ?? other.WorkingDir;
            self.PathExtension = self.PathExtension ?? other.PathExtension;
            self.BatchForTestSetup = self.BatchForTestSetup ?? other.BatchForTestSetup;
            self.BatchForTestTeardown = self.BatchForTestTeardown ?? other.BatchForTestTeardown;
            self.TraitsRegexesAfter = self.TraitsRegexesAfter ?? other.TraitsRegexesAfter;
            self.TraitsRegexesBefore = self.TraitsRegexesBefore ?? other.TraitsRegexesBefore;
            self.TestNameSeparator = self.TestNameSeparator ?? other.TestNameSeparator;
            self.ParseSymbolInformation = self.ParseSymbolInformation ?? other.ParseSymbolInformation;
            self.DebugMode = self.DebugMode ?? other.DebugMode;
            self.TimestampOutput = self.TimestampOutput ?? other.TimestampOutput;
            self.ShowReleaseNotes = self.ShowReleaseNotes ?? other.ShowReleaseNotes;
            self.KillProcessesOnCancel = self.KillProcessesOnCancel ?? other.KillProcessesOnCancel;

            self.UseNewTestExecutionFramework = self.UseNewTestExecutionFramework ?? other.UseNewTestExecutionFramework;

            self.DebuggingNamedPipeId = self.DebuggingNamedPipeId ?? other.DebuggingNamedPipeId;
        }
    }

}