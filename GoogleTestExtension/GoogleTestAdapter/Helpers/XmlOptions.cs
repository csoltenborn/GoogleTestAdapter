using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

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
    }

    public static class XmlOptionsExtension
    {
        public static void GetUnsetValuesFrom(this IXmlOptions self, IXmlOptions other)
        {
            self.AdditionalTestExecutionParam   = self.AdditionalTestExecutionParam ?? other.AdditionalTestExecutionParam;
            self.MaxNrOfThreads                 = self.MaxNrOfThreads               ?? other.MaxNrOfThreads;
            self.NrOfTestRepetitions            = self.NrOfTestRepetitions          ?? other.NrOfTestRepetitions;
            self.ParallelTestExecution          = self.ParallelTestExecution        ?? other.ParallelTestExecution;
            self.PrintTestOutput                = self.PrintTestOutput              ?? other.PrintTestOutput;
            self.ReportWaitPeriod               = self.ReportWaitPeriod             ?? other.ReportWaitPeriod;
            self.RunDisabledTests               = self.RunDisabledTests             ?? other.RunDisabledTests;
            self.ShuffleTests                   = self.ShuffleTests                 ?? other.ShuffleTests;
            self.ShuffleTestsSeed               = self.ShuffleTestsSeed             ?? other.ShuffleTestsSeed;
            self.TestDiscoveryRegex             = self.TestDiscoveryRegex           ?? other.TestDiscoveryRegex;
            self.BatchForTestSetup              = self.BatchForTestSetup            ?? other.BatchForTestSetup;
            self.BatchForTestTeardown           = self.BatchForTestTeardown         ?? other.BatchForTestTeardown;
            self.TraitsRegexesAfter             = self.TraitsRegexesAfter           ?? other.TraitsRegexesAfter;
            self.TraitsRegexesBefore            = self.TraitsRegexesBefore          ?? other.TraitsRegexesBefore;
            self.DebugMode                      = self.DebugMode                    ?? other.DebugMode;
        }
    }


    [XmlRoot(GoogleTestConstants.SettingsName)]
    public class RunSettings : TestRunSettings, IXmlOptions
    {
        public RunSettings()
            : base(GoogleTestConstants.SettingsName)
        { }

        public bool? PrintTestOutput { get; set; }
        public bool ShouldSerializePrintTestOutput() { return PrintTestOutput != null; }

        public string TestDiscoveryRegex { get; set; }
        public bool ShouldSerializeTestDiscoveryRegex() { return TestDiscoveryRegex != null; }

        public bool? RunDisabledTests { get; set; }
        public bool ShouldSerializeRunDisabledTests() { return RunDisabledTests != null; }

        public int? NrOfTestRepetitions { get; set; }
        public bool ShouldSerializeNrOfTestRepetitions() { return NrOfTestRepetitions != null; }

        public bool? ShuffleTests { get; set; }
        public bool ShouldSerializeShuffleTests() { return ShuffleTests != null; }

        public int? ShuffleTestsSeed { get; set; }
        public bool ShouldSerializeShuffleTestsSeed() { return ShuffleTestsSeed != null; }

        public string TraitsRegexesBefore { get; set; }
        public bool ShouldSerializeTraitsRegexesBefore() { return TraitsRegexesBefore != null; }

        public string TraitsRegexesAfter { get; set; }
        public bool ShouldSerializeTraitsRegexesAfter() { return TraitsRegexesAfter != null; }

        public bool? DebugMode { get; set; }
        public bool ShouldSerializeDebugMode() { return DebugMode != null; }

        public string AdditionalTestExecutionParam { get; set; }
        public bool ShouldSerializeAdditionalTestExecutionParam() { return AdditionalTestExecutionParam != null; }

        public bool? ParallelTestExecution { get; set; }
        public bool ShouldSerializeParallelTestExecution() { return ParallelTestExecution != null; }

        public int? MaxNrOfThreads { get; set; }
        public bool ShouldSerializeMaxNrOfThreads() { return MaxNrOfThreads != null; }

        public string BatchForTestSetup { get; set; }
        public bool ShouldSerializeBatchForTestSetup() { return BatchForTestSetup != null; }

        public string BatchForTestTeardown { get; set; }
        public bool ShouldSerializeBatchForTestTeardown() { return BatchForTestTeardown != null; }

        public int? ReportWaitPeriod { get; set; }
        public bool ShouldSerializeReportWaitPeriod() { return ReportWaitPeriod != null; }


        public override XmlElement ToXml()
        {
            var document = new XmlDocument();
            using (XmlWriter writer = document.CreateNavigator().AppendChild())
            {
                new XmlSerializer(GetType()).Serialize(writer, this);
            }
            return document.DocumentElement;
        }

        public static RunSettings LoadFromXml(XmlReader reader)
        {
            ValidateArg.NotNull(reader, nameof(reader));

            var runSettings = new RunSettings();
            if (reader.Read() && reader.Name.Equals(GoogleTestConstants.SettingsName))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(RunSettings));
                    runSettings = serializer.Deserialize(reader) as RunSettings;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            return runSettings;
        }

    }

}