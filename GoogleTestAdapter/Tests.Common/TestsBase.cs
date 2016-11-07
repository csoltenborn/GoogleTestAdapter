using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.Tests.Common
{
    public abstract class TestsBase
    {
        protected readonly Mock<ILogger> MockLogger;
        protected readonly Mock<SettingsWrapper> MockOptions;
        protected readonly Mock<ITestFrameworkReporter> MockFrameworkReporter;

        protected readonly TestDataCreator TestDataCreator;

        protected readonly TestEnvironment TestEnvironment;

        protected TestsBase()
        {
            MockLogger = new Mock<ILogger>();
            Mock<IGoogleTestAdapterSettingsContainer> mockSettingsContainer = new Mock<IGoogleTestAdapterSettingsContainer>();
            MockOptions = new Mock<SettingsWrapper>(mockSettingsContainer.Object);
            MockOptions.Setup(o => o.Clone()).Returns(MockOptions.Object);
            MockFrameworkReporter = new Mock<ITestFrameworkReporter>();

            TestEnvironment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
            TestDataCreator = new TestDataCreator(TestEnvironment);
        }


        [TestInitialize]
        public virtual void SetUp()
        {
            SetupOptions(MockOptions);
        }

        public static void SetupOptions(Mock<SettingsWrapper> mockOptions)
        {
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
            mockOptions.Setup(o => o.DebugMode).Returns(SettingsWrapper.OptionDebugModeDefaultValue);
            mockOptions.Setup(o => o.TimestampOutput).Returns(SettingsWrapper.OptionTimestampOutputDefaultValue);
            mockOptions.Setup(o => o.ShowReleaseNotes).Returns(SettingsWrapper.OptionShowReleaseNotesDefaultValue);
            mockOptions.Setup(o => o.AdditionalTestExecutionParam)
                .Returns(SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue);
            mockOptions.Setup(o => o.BatchForTestSetup).Returns(SettingsWrapper.OptionBatchForTestSetupDefaultValue);
            mockOptions.Setup(o => o.BatchForTestTeardown).Returns(SettingsWrapper.OptionBatchForTestTeardownDefaultValue);
            mockOptions.Setup(o => o.ParallelTestExecution)
                .Returns(SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);
            mockOptions.Setup(o => o.MaxNrOfThreads).Returns(SettingsWrapper.OptionMaxNrOfThreadsDefaultValue);
            mockOptions.Setup(o => o.PathExtension).Returns(SettingsWrapper.OptionPathExtensionDefaultValue);
            mockOptions.Setup(o => o.WorkingDir).Returns(SettingsWrapper.OptionWorkingDirDefaultValue);

            mockOptions.Setup(o => o.UseNewTestExecutionFramework).Returns(true);
            mockOptions.Setup(o => o.VisualStudioProcessId).Returns(-1);
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