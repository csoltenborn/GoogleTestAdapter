using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class SequentialTestRunnerTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CancelingDuringTestExecution_StopsTestExecution()
        {
            DoRunCancelingTests(
                false, 
                2000,  // 1st test should be executed
                3000); // 2nd test should not be executed 
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CancelingAndKillingProcessesDuringTestExecution_StopsTestExecutionFaster()
        {
            DoRunCancelingTests(
                true,
                1000,  // 1st test should be canceled
                2000); // 2nd test should not be executed 
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_WorkingDirNotSet_TestFails()
        {
            var testCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            var settings = CreateSettings(null, null);
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Failed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_WorkingDirSetForSolution_TestPasses()
        {
            var testCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            var settings = CreateSettings(PlaceholderReplacer.SolutionDirPlaceholder, null);
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_WorkingDirSetForProject_TestPasses()
        {
            TestCase testCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            var settings = CreateSettings("foo", PlaceholderReplacer.SolutionDirPlaceholder);
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_EnvironmentVariableSetForSolution_TestPasses()
        {
            TestCase testCase = TestDataCreator.GetTestCases("EnvironmentVariable.IsSet").First();
            var settings = CreateSettings(PlaceholderReplacer.SolutionDirPlaceholder, null, "MYENVVAR=MyValue");

            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Once);
        }

        private void DoRunCancelingTests(bool killProcesses, int lower, int upper)
        {
            MockOptions.Setup(o => o.KillProcessesOnCancel).Returns(killProcesses);
            List<TestCase> testCasesToRun = TestDataCreator.GetTestCases("Crashing.LongRunning", "LongRunningTests.Test2");

            var stopwatch = new Stopwatch();
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, TestEnvironment.Options, new SchedulingAnalyzer(TestEnvironment.Logger));
            var thread = new Thread(() => runner.RunTests(testCasesToRun, false, ProcessExecutorFactory));

            stopwatch.Start();
            thread.Start();
            Thread.Sleep(1000);
            runner.Cancel();
            thread.Join();
            stopwatch.Stop();

            testCasesToRun.Should().HaveCount(2);
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);

            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(lower); // 1st test should be executed
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(upper); // 2nd test should not be executed 
        }

        private SettingsWrapper CreateSettings(string solutionWorkingDir, string projectWorkingDir, string environmentVariable = null)
        {
            var mockContainer = new Mock<IGoogleTestAdapterSettingsContainer>();

            var solutionSettings = new RunSettings {WorkingDir = solutionWorkingDir};
            mockContainer
                .Setup(c => c.SolutionSettings)
                .Returns(solutionSettings);

            if (projectWorkingDir != null)
            {
                mockContainer
                    .Setup(c => c.GetSettingsForExecutable(It.IsAny<string>()))
                    .Returns(new RunSettings { WorkingDir = projectWorkingDir });
            }

            if (environmentVariable != null)
            {
                solutionSettings.EnvironmentVariables = environmentVariable;
            }

            return new SettingsWrapper(mockContainer.Object, TestResources.SampleTestsSolutionDir)
            {
                RegexTraitParser = new RegexTraitParser(MockLogger.Object),
                EnvironmentVariablesParser = new EnvironmentVariablesParser(MockLogger.Object),
                HelperFilesCache = new HelperFilesCache(MockLogger.Object)
            };
        }

        private bool CheckSingleResultHasOutcome(IEnumerable<TestResult> testResults, TestOutcome outcome)
        {
            return testResults.SingleOrDefault()?.Outcome == outcome;
        }

    }

}