using System.Collections.Generic;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter
{
    public abstract class AbstractCoreTests
    {
        protected readonly TestEnvironment TestEnvironment;

        protected readonly Mock<ILogger> MockLogger;
        protected readonly Mock<SettingsWrapper> MockOptions;
        protected readonly Mock<ITestFrameworkReporter> MockFrameworkReporter;

        protected readonly TestDataCreator TestDataCreator;

        protected AbstractCoreTests()
        {
            MockLogger = new Mock<ILogger>();
            MockOptions = new Mock<SettingsWrapper>();
            MockFrameworkReporter = new Mock<ITestFrameworkReporter>();

            TestEnvironment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
            TestDataCreator = new TestDataCreator(TestEnvironment);
        }


        [TestInitialize]
        public virtual void SetUp()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.TestNameSeparator).Returns(SettingsWrapper.OptionTestNameSeparatorDefaultValue);
            MockOptions.Setup(o => o.NrOfTestRepetitions).Returns(SettingsWrapper.OptionNrOfTestRepetitionsDefaultValue);
            MockOptions.Setup(o => o.PrintTestOutput).Returns(SettingsWrapper.OptionPrintTestOutputDefaultValue);
            MockOptions.Setup(o => o.CatchExceptions).Returns(SettingsWrapper.OptionCatchExceptionsDefaultValue);
            MockOptions.Setup(o => o.BreakOnFailure).Returns(SettingsWrapper.OptionBreakOnFailureDefaultValue);
            MockOptions.Setup(o => o.RunDisabledTests).Returns(SettingsWrapper.OptionRunDisabledTestsDefaultValue);
            MockOptions.Setup(o => o.ShuffleTests).Returns(SettingsWrapper.OptionShuffleTestsDefaultValue);
            MockOptions.Setup(o => o.ShuffleTestsSeed).Returns(SettingsWrapper.OptionShuffleTestsSeedDefaultValue);
            MockOptions.Setup(o => o.ParseSymbolInformation).Returns(SettingsWrapper.OptionParseSymbolInformationDefaultValue);
            MockOptions.Setup(o => o.DebugMode).Returns(SettingsWrapper.OptionDebugModeDefaultValue);
            MockOptions.Setup(o => o.TimestampOutput).Returns(SettingsWrapper.OptionTimestampOutputDefaultValue);
            MockOptions.Setup(o => o.ShowReleaseNotes).Returns(SettingsWrapper.OptionShowReleaseNotesDefaultValue);
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue);
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(SettingsWrapper.OptionBatchForTestSetupDefaultValue);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(SettingsWrapper.OptionBatchForTestTeardownDefaultValue);
            MockOptions.Setup(o => o.ParallelTestExecution).Returns(SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(SettingsWrapper.OptionMaxNrOfThreadsDefaultValue);
            MockOptions.Setup(o => o.PathExtension).Returns(SettingsWrapper.OptionPathExtensionDefaultValue);
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