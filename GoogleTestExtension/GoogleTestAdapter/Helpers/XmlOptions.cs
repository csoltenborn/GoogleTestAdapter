namespace GoogleTestAdapter.Helpers
{

    public interface IXmlOptions
    {
        string AdditionalTestExecutionParam { get; set; }
        int? MaxNrOfThreads { get; set; }
        int? NrOfTestRepetitions { get; set; }
        bool? ParallelTestExecution { get; set; }
        bool? PrintTestOutput { get; set; }
        int? ReportWaitPeriod { get; set; }
        bool? RunDisabledTests { get; set; }
        bool? ShuffleTests { get; set; }
        int? ShuffleTestsSeed { get; set; }
        string TestDiscoveryRegex { get; set; }
        string BatchForTestSetup { get; set; }
        string BatchForTestTeardown { get; set; }
        string TraitsRegexesAfter { get; set; }
        string TraitsRegexesBefore { get; set; }
        bool? DebugMode { get; set; }
        bool? DevelopmentMode { get; set; }
    }

    public static class XmlOptionsExtension
    {
        public static void GetUnsetValuesFrom(this IXmlOptions self, IXmlOptions other)
        {
            self.AdditionalTestExecutionParam = self.AdditionalTestExecutionParam ?? other.AdditionalTestExecutionParam;
            self.MaxNrOfThreads = self.MaxNrOfThreads ?? other.MaxNrOfThreads;
            self.NrOfTestRepetitions = self.NrOfTestRepetitions ?? other.NrOfTestRepetitions;
            self.ParallelTestExecution = self.ParallelTestExecution ?? other.ParallelTestExecution;
            self.PrintTestOutput = self.PrintTestOutput ?? other.PrintTestOutput;
            self.ReportWaitPeriod = self.ReportWaitPeriod ?? other.ReportWaitPeriod;
            self.RunDisabledTests = self.RunDisabledTests ?? other.RunDisabledTests;
            self.ShuffleTests = self.ShuffleTests ?? other.ShuffleTests;
            self.ShuffleTestsSeed = self.ShuffleTestsSeed ?? other.ShuffleTestsSeed;
            self.TestDiscoveryRegex = self.TestDiscoveryRegex ?? other.TestDiscoveryRegex;
            self.BatchForTestSetup = self.BatchForTestSetup ?? other.BatchForTestSetup;
            self.BatchForTestTeardown = self.BatchForTestTeardown ?? other.BatchForTestTeardown;
            self.TraitsRegexesAfter = self.TraitsRegexesAfter ?? other.TraitsRegexesAfter;
            self.TraitsRegexesBefore = self.TraitsRegexesBefore ?? other.TraitsRegexesBefore;
            self.DebugMode = self.DebugMode ?? other.DebugMode;
            self.DevelopmentMode = self.DevelopmentMode ?? other.DevelopmentMode;
        }
    }

}