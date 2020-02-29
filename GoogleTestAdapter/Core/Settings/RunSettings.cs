// This file has been modified by Microsoft on 6/2017.

using System.Xml.Serialization;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution;

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

        public virtual string AdditionalPdbs { get; set; }
        public bool ShouldSerializeAdditionalPdbs() { return AdditionalPdbs != null; }

        public virtual int? TestDiscoveryTimeoutInSeconds { get; set; }
        public bool ShouldSerializeTestDiscoveryTimeoutInSeconds() { return TestDiscoveryTimeoutInSeconds != null; }

        public virtual string WorkingDir { get; set; }
        public bool ShouldSerializeWorkingDir() { return WorkingDir != null; }

        public virtual string PathExtension { get; set; }
        public bool ShouldSerializePathExtension() { return PathExtension != null; }

        public virtual string EnvironmentVariables { get; set; }
        public bool ShouldSerializeEnvironmentVariables() { return EnvironmentVariables != null; }

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

        public virtual OutputMode? OutputMode { get; set; }
        public bool ShouldSerializeOutputMode() { return OutputMode != null; }

        public virtual bool? TimestampOutput { get; set; }
        public bool ShouldSerializeTimestampOutput() { return TimestampOutput != null; }

        public virtual TimestampMode? TimestampMode { get; set; }
        public bool ShouldSerializeTimestampMode() { return TimestampMode != null; }

        public virtual SeverityMode? SeverityMode { get; set; }
        public bool ShouldSerializeSeverityMode() { return SeverityMode != null; }

        public virtual SummaryMode? SummaryMode { get; set; }
        public bool ShouldSerializeSummaryMode() { return SummaryMode != null; }

        public virtual bool? PrefixOutputWithGta { get; set; }
        public bool ShouldSerializePrefixOutputWithGta() { return PrefixOutputWithGta != null; }

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

        public bool? SkipOriginCheck { get; set; }
        public bool ShouldSerializeSkipOriginCheck() { return SkipOriginCheck != null; }

        public string ExitCodeTestCase { get; set; }
        public bool ShouldSerializeExitCodeTestCase() { return ExitCodeTestCase != null; }

        public MissingTestsReportMode? MissingTestsReportMode { get; set; }
        public bool ShouldSerializeMissingTestsReportMode() { return MissingTestsReportMode != null; }


        public virtual bool? UseNewTestExecutionFramework { get; set; }
        public bool ShouldSerializeUseNewTestExecutionFramework() { return UseNewTestExecutionFramework != null; }

        public virtual DebuggerKind? DebuggerKind { get; set; }
        public bool ShouldSerializeDebuggerKind() { return DebuggerKind != null; }


        // internal
        public virtual string DebuggingNamedPipeId { get; set; }
        public bool ShouldSerializeDebuggingNamedPipeId() { return DebuggingNamedPipeId != null; }

        public virtual string SolutionDir { get; set; }
        public bool ShouldSerializeSolutionDir() { return SolutionDir != null; }

        public virtual string PlatformName { get; set; }
        public bool ShouldSerializePlatformName() { return PlatformName != null; }

        public virtual string ConfigurationName { get; set; }
        public bool ShouldSerializeConfigurationName() { return ConfigurationName != null; }

    }

}