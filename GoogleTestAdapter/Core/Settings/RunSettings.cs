// This file has been modified by Microsoft on 6/2017.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace GoogleTestAdapter.Settings
{

    public class RunSettings : IGoogleTestAdapterSettings
    {
        public RunSettings() : this(null) {}

        public RunSettings(string projectRegex)
        {
            ProjectRegex = projectRegex;
        }

        [XmlAttribute]
        public string ProjectRegex { get; set; }

        public virtual bool? PrintTestOutput { get; set; }
        public bool ShouldSerializePrintTestOutput() { return PrintTestOutput != null; }

        public virtual string TestDiscoveryRegex { get; set; }
        public bool ShouldSerializeTestDiscoveryRegex() { return TestDiscoveryRegex != null; }

        public virtual int? TestDiscoveryTimeoutInSeconds { get; set; }
        public bool ShouldSerializeTestDiscoveryTimeoutInSeconds() { return TestDiscoveryTimeoutInSeconds != null; }

        public virtual string WorkingDir { get; set; }
        public bool ShouldSerializeWorkingDir() { return WorkingDir != null; }

        public virtual string PathExtension { get; set; }
        public bool ShouldSerializePathExtension() { return PathExtension != null; }

        public virtual bool? CatchExceptions { get; set; }
        public bool ShouldSerializeCatchExceptions() { return CatchExceptions != null; }

        public virtual bool? BreakOnFailure { get; set; }
        public bool ShouldSerializeBreakOnFailure() { return BreakOnFailure != null; }

        public virtual bool? RunDisabledTests { get; set; }
        public bool ShouldSerializeRunDisabledTests() { return RunDisabledTests != null; }

        public virtual int? NrOfTestRepetitions { get; set; }
        public bool ShouldSerializeNrOfTestRepetitions() { return NrOfTestRepetitions != null; }

        public virtual bool? ShuffleTests { get; set; }
        public bool ShouldSerializeShuffleTests() { return ShuffleTests != null; }

        public virtual int? ShuffleTestsSeed { get; set; }
        public bool ShouldSerializeShuffleTestsSeed() { return ShuffleTestsSeed != null; }

        public virtual string TraitsRegexesBefore { get; set; }
        public bool ShouldSerializeTraitsRegexesBefore() { return TraitsRegexesBefore != null; }

        public virtual string TraitsRegexesAfter { get; set; }
        public bool ShouldSerializeTraitsRegexesAfter() { return TraitsRegexesAfter != null; }

        public virtual string TestNameSeparator { get; set; }
        public bool ShouldSerializeTestNameSeparator() { return TestNameSeparator != null; }

        public virtual bool? DebugMode { get; set; }
        public bool ShouldSerializeDebugMode() { return DebugMode != null; }

        public virtual bool? TimestampOutput { get; set; }
        public bool ShouldSerializeTimestampOutput() { return TimestampOutput != null; }

        public virtual bool? ShowReleaseNotes { get; set; }
        public bool ShouldSerializeShowReleaseNotes() { return ShowReleaseNotes != null; }

        public virtual bool? ParseSymbolInformation { get; set; }
        public bool ShouldSerializeParseSymbolInformation() { return ParseSymbolInformation != null; }

        public virtual string AdditionalTestExecutionParam { get; set; }
        public bool ShouldSerializeAdditionalTestExecutionParam() { return AdditionalTestExecutionParam != null; }

        public virtual bool? ParallelTestExecution { get; set; }
        public bool ShouldSerializeParallelTestExecution() { return ParallelTestExecution != null; }

        public virtual int? MaxNrOfThreads { get; set; }
        public bool ShouldSerializeMaxNrOfThreads() { return MaxNrOfThreads != null; }

        public virtual string BatchForTestSetup { get; set; }
        public bool ShouldSerializeBatchForTestSetup() { return BatchForTestSetup != null; }

        public virtual string BatchForTestTeardown { get; set; }
        public bool ShouldSerializeBatchForTestTeardown() { return BatchForTestTeardown != null; }

        public virtual bool? KillProcessesOnCancel { get; set; }
        public bool ShouldSerializeKillProcessesOnCancel() { return KillProcessesOnCancel != null; }


        public virtual bool? UseNewTestExecutionFramework { get; set; }
        public bool ShouldSerializeUseNewTestExecutionFramework() { return UseNewTestExecutionFramework != null; }


        // internal
        public string DebuggingNamedPipeId { get; set; }
        public bool ShouldSerializeDebuggingNamedPipeId() { return DebuggingNamedPipeId != null; }

    }

}