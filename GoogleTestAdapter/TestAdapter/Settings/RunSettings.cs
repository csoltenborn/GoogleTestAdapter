using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter.Settings
{

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

        public string PathExtension { get; set; }
        public bool ShouldSerializePathExtension() { return PathExtension != null; }

        public bool? CatchExceptions { get; set; }
        public bool ShouldSerializeCatchExceptions() { return CatchExceptions != null; }

        public bool? BreakOnFailure { get; set; }
        public bool ShouldSerializeBreakOnFailure() { return BreakOnFailure != null; }

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

        public string TestNameSeparator { get; set; }
        public bool ShouldSerializeTestNameSeparator() { return TestNameSeparator != null; }

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