namespace GoogleTestAdapter.Helpers
{

    /*
    To add a new option, make the following changes:
    - add (nullable!) property to GoogleTestAdapter.Helpers.IXMLOptions
    - handle property in method GoogleTestAdapter.Helpers.XmlOptionsExtension.GetUnsetValuesFrom()
    - add property and according constants to class GoogleTestAdapter.Options
    - handle property serialization in class GoogleTestAdapter.TestAdapter.Settings.RunSettings
    - add Options UI integration to one of the classes in GoogleTestAdapter.VsPackage.OptionsPages.*
    - handle property in method GoogleTestAdapter.VsPackage.GoogleTestExtensionOptionsPage.GetRunSettingsFromOptionPages()
    - add new option to Resources/AllTestSettings.gta.runsettings
    - add default mock configuration in method AbstractGoogleTestExtensionTests.SetUp()
    */
    public interface IXmlOptions
    {
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
        string PathExtension { get; set; }
        string BatchForTestSetup { get; set; }
        string BatchForTestTeardown { get; set; }
        string TraitsRegexesAfter { get; set; }
        string TraitsRegexesBefore { get; set; }
        string TestNameSeparator { get; set; }
        bool? DebugMode { get; set; }
    }

    public static class XmlOptionsExtension
    {
        public static void GetUnsetValuesFrom(this IXmlOptions self, IXmlOptions other)
        {
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
            self.PathExtension = self.PathExtension ?? other.PathExtension;
            self.BatchForTestSetup = self.BatchForTestSetup ?? other.BatchForTestSetup;
            self.BatchForTestTeardown = self.BatchForTestTeardown ?? other.BatchForTestTeardown;
            self.TraitsRegexesAfter = self.TraitsRegexesAfter ?? other.TraitsRegexesAfter;
            self.TraitsRegexesBefore = self.TraitsRegexesBefore ?? other.TraitsRegexesBefore;
            self.TestNameSeparator = self.TestNameSeparator ?? other.TestNameSeparator;
            self.DebugMode = self.DebugMode ?? other.DebugMode;
        }
    }

}