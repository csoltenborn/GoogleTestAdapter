using System;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.Tests.Common
{
    public abstract class TestsBase
    {
        private class DummyProcessFactory : ProcessExecutorFactory, IDebuggedProcessExecutorFactory
        {
            public IDebuggedProcessExecutor CreateFrameworkDebuggingExecutor(bool printTestOutput, ILogger logger)
            {
                throw new NotImplementedException();
            }

            public IDebuggedProcessExecutor CreateNativeDebuggingExecutor(DebuggerEngine engine, bool printTestOutput, ILogger logger)
            {
                throw new NotImplementedException();
            }
        }

        protected readonly Mock<ILogger> MockLogger;
        protected readonly Mock<SettingsWrapper> MockOptions;
        protected readonly Mock<ITestFrameworkReporter> MockFrameworkReporter;

        protected readonly TestDataCreator TestDataCreator;

        protected readonly TestEnvironment TestEnvironment;

        protected IDebuggedProcessExecutorFactory ProcessExecutorFactory => new DummyProcessFactory();

        protected TestsBase()
        {
            MockLogger = new Mock<ILogger>();
            MockLogger.Setup(l => l.GetMessages(It.IsAny<Severity[]>())).Returns(new List<string>());

            var mockSettingsContainer = new Mock<IGoogleTestAdapterSettingsContainer>();
            var mockRunSettings = new Mock<RunSettings>();
            mockSettingsContainer.Setup(c => c.SolutionSettings).Returns(mockRunSettings.Object);
            MockOptions = new Mock<SettingsWrapper>(mockSettingsContainer.Object, Path.GetFullPath(TestResources.SampleTestsSolutionDir));

            MockFrameworkReporter = new Mock<ITestFrameworkReporter>();

            TestEnvironment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
            TestDataCreator = new TestDataCreator(TestEnvironment);
        }


        [TestInitialize]
        public virtual void SetUp()
        {
            SetupOptions(MockOptions, MockLogger.Object);
        }

        public static void SetupOptions(Mock<SettingsWrapper> mockOptions, ILogger logger)
        {
            mockOptions.Object.HelperFilesCache = new HelperFilesCache(logger);
            mockOptions.Object.EnvironmentVariablesParser = new EnvironmentVariablesParser(logger);

            mockOptions.Setup(o => o.CheckCorrectUsage(It.IsAny<string>())).Callback(() => { });
            mockOptions.Setup(o => o.Clone()).Returns(mockOptions.Object);

            mockOptions.Setup(o => o.AdditionalPdbs).Returns(SettingsWrapper.OptionAdditionalPdbsDefaultValue);
            mockOptions.Setup(o => o.TestDiscoveryTimeoutInSeconds)
                .Returns(SettingsWrapper.OptionTestDiscoveryTimeoutInSecondsDefaultValue);
            mockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new List<RegexTraitPair>());
            mockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new List<RegexTraitPair>());
            mockOptions.Setup(o => o.TestNameSeparator).Returns(SettingsWrapper.OptionTestNameSeparatorDefaultValue);
            mockOptions.Setup(o => o.NrOfTestRepetitions).Returns(SettingsWrapper.OptionNrOfTestRepetitionsDefaultValue);
            mockOptions.Setup(o => o.PrintTestOutput).Returns(SettingsWrapper.OptionPrintTestOutputDefaultValue);
            mockOptions.Setup(o => o.CatchExceptions).Returns(SettingsWrapper.OptionCatchExceptionsDefaultValue);
            mockOptions.Setup(o => o.BreakOnFailure).Returns(SettingsWrapper.OptionBreakOnFailureDefaultValue);
            mockOptions.Setup(o => o.RunDisabledTests).Returns(SettingsWrapper.OptionRunDisabledTestsDefaultValue);
            mockOptions.Setup(o => o.ShuffleTests).Returns(SettingsWrapper.OptionShuffleTestsDefaultValue);
            mockOptions.Setup(o => o.ShuffleTestsSeed).Returns(SettingsWrapper.OptionShuffleTestsSeedDefaultValue);
            mockOptions.Setup(o => o.ParseSymbolInformation).Returns(SettingsWrapper.OptionParseSymbolInformationDefaultValue);
            mockOptions.Setup(o => o.OutputMode).Returns(SettingsWrapper.OptionOutputModeDefaultValue);
            mockOptions.Setup(o => o.TimestampMode).Returns(TimestampMode.DoNotPrintTimestamp);
            mockOptions.Setup(o => o.SeverityMode).Returns(SeverityMode.PrintSeverity);
            mockOptions.Setup(o => o.SummaryMode).Returns(SettingsWrapper.OptionSummaryModeDefaultValue);
            mockOptions.Setup(o => o.PrefixOutputWithGta)
                .Returns(SettingsWrapper.OptionPrefixOutputWithGtaDefaultValue);
            mockOptions.Setup(o => o.AdditionalTestExecutionParam)
                .Returns(SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue);
            mockOptions.Setup(o => o.BatchForTestSetup).Returns(SettingsWrapper.OptionBatchForTestSetupDefaultValue);
            mockOptions.Setup(o => o.BatchForTestTeardown).Returns(SettingsWrapper.OptionBatchForTestTeardownDefaultValue);
            mockOptions.Setup(o => o.ParallelTestExecution)
                .Returns(SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);
            mockOptions.Setup(o => o.MaxNrOfThreads).Returns(SettingsWrapper.OptionMaxNrOfThreadsDefaultValue);
            mockOptions.Setup(o => o.PathExtension).Returns(SettingsWrapper.OptionPathExtensionDefaultValue);
            mockOptions.Setup(o => o.EnvironmentVariables).Returns(SettingsWrapper.OptionEnvironmentVariablesDefaultValue);
            mockOptions.Setup(o => o.WorkingDir).Returns(SettingsWrapper.OptionWorkingDirDefaultValue);
            mockOptions.Setup(o => o.KillProcessesOnCancel).Returns(SettingsWrapper.OptionKillProcessesOnCancelDefaultValue);
            mockOptions.Setup(o => o.SkipOriginCheck).Returns(SettingsWrapper.OptionSkipOriginCheckDefaultValue);
            mockOptions.Setup(o => o.ExitCodeTestCase).Returns(SettingsWrapper.OptionExitCodeTestCaseDefaultValue);
            mockOptions.Setup(o => o.MissingTestsReportMode)
                .Returns(SettingsWrapper.OptionMissingTestsReportModeDefaultValue);

            mockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.Native);

            mockOptions.Setup(o => o.DebuggingNamedPipeId).Returns(Guid.NewGuid().ToString());
            mockOptions.Setup(o => o.SolutionDir).CallBase();
        }

        [TestCleanup]
        public virtual void TearDown()
        {
            MockLogger.Reset();
            MockOptions.Reset();
            MockFrameworkReporter.Reset();
        }

    }

}