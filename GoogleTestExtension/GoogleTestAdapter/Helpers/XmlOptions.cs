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
        int MaxNrOfThreads { get; set; }
        int NrOfTestRepetitions { get; set; }
        bool ParallelTestExecution { get; set; }
        bool PrintTestOutput { get; set; }
        int ReportWaitPeriod { get; set; }
        bool RunDisabledTests { get; set; }
        bool ShuffleTests { get; set; }
        int ShuffleTestsSeed { get; set; }
        string TestDiscoveryRegex { get; set; }
        string BatchForTestSetup { get; set; }
        string BatchForTestTeardown { get; set; }
        string TraitsRegexesAfter { get; set; }
        string TraitsRegexesBefore { get; set; }
        bool UserDebugMode { get; set; }
    }

    [XmlRoot(GoogleTestConstants.SettingsName)]
    public class RunSettings : TestRunSettings, IXmlOptions
    {
        public RunSettings()
            : base(GoogleTestConstants.SettingsName)
        { }

        public bool PrintTestOutput { get; set; } = Options.OptionPrintTestOutputDefaultValue;
        public string TestDiscoveryRegex { get; set; } = Options.OptionTestDiscoveryRegexDefaultValue;
        public bool RunDisabledTests { get; set; } = Options.OptionRunDisabledTestsDefaultValue;
        public int NrOfTestRepetitions { get; set; } = Options.OptionNrOfTestRepetitionsDefaultValue;
        public bool ShuffleTests { get; set; } = Options.OptionShuffleTestsDefaultValue;
        public int ShuffleTestsSeed { get; set; } = Options.OptionShuffleTestsSeedDefaultValue;
        public string TraitsRegexesBefore { get; set; } = Options.OptionTraitsRegexesDefaultValue;
        public string TraitsRegexesAfter { get; set; } = Options.OptionTraitsRegexesDefaultValue;
        public bool UserDebugMode { get; set; } = Options.OptionUserDebugModeDefaultValue;
        public string AdditionalTestExecutionParam { get; set; } = Options.OptionAdditionalTestExecutionParamsDefaultValue;

        public bool ParallelTestExecution { get; set; } = Options.OptionEnableParallelTestExecutionDefaultValue;
        public int MaxNrOfThreads { get; set; } = Options.OptionMaxNrOfThreadsDefaultValue;
        public string BatchForTestSetup { get; set; } = Options.OptionBatchForTestSetupDefaultValue;
        public string BatchForTestTeardown { get; set; } = Options.OptionBatchForTestTeardownDefaultValue;

        public int ReportWaitPeriod { get; set; } = Options.OptionReportWaitPeriodDefaultValue;

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

            RunSettings runSettings = new RunSettings();
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