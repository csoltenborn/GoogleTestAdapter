using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestResults;
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
        public void RunTests_CancelingWhileExecutorIsCreated_ExecutableIsNotStarted()
        {
            MockOptions.Setup(o => o.KillProcessesOnCancel).Returns(true);
            List<TestCase> testCasesToRun = TestDataCreator.GetTestCases("Crashing.LongRunning");
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, TestEnvironment.Options, new SchedulingAnalyzer(TestEnvironment.Logger));

            // the cancel request arrives after the runner has checked for it, but before the executable is started
            var mockFactory = new Mock<IDebuggedProcessExecutorFactory>();
            mockFactory
                .Setup(f => f.CreateExecutor(It.IsAny<bool>(), It.IsAny<ILogger>()))
                .Returns((bool printTestOutput, ILogger logger) =>
                {
                    runner.Cancel();
                    return new DotNetProcessExecutor(printTestOutput, logger);
                });

            var stopwatch = Stopwatch.StartNew();
            runner.RunTests(testCasesToRun, false, mockFactory.Object);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Crashing.LongRunning takes 2s
            runner.ExecutableResults.Should().ContainSingle().Which.ExitCode.Should().Be(int.MaxValue);
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

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CMakeWorkingDirectory_TestPasses()
        {
            var testCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            var settings = CreateSettings(null, null);
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                CreateTestProperties(testCase, TestResources.SampleTestsSolutionDir));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CMakeWorkingDirectoryAndConfiguredWorkingDir_ConfiguredWorkingDirWinsAndTestFails()
        {
            var testCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            var settings = CreateSettings(Path.GetTempPath(), null);
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                CreateTestProperties(testCase, TestResources.SampleTestsSolutionDir));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Failed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CMakeEnvironment_TestPasses()
        {
            TestCase testCase = TestDataCreator.GetTestCases("EnvironmentVariable.IsSet").First();
            var settings = CreateSettings(null, null);
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                CreateTestProperties(testCase, null, "MYENVVAR", "MyValue"));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CMakeEnvironmentAndConfiguredEnvironmentVariable_ConfiguredVariableWinsAndTestPasses()
        {
            TestCase testCase = TestDataCreator.GetTestCases("EnvironmentVariable.IsSet").First();
            var settings = CreateSettings(null, null, "MYENVVAR=MyValue");
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                CreateTestProperties(testCase, null, "MYENVVAR", "WrongValue"));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(testCase.Yield(), false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_TestsWithDifferentCMakeTestProperties_TestsAreRunSeparatelyAndPass()
        {
            TestCase workingDirTestCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            TestCase environmentTestCase = TestDataCreator.GetTestCases("EnvironmentVariable.IsSet").First();
            workingDirTestCase.Source.Should().Be(environmentTestCase.Source);
            var settings = CreateSettings(null, null);
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                CreateTestProperties(workingDirTestCase, TestResources.SampleTestsSolutionDir),
                CreateTestProperties(environmentTestCase, null, "MYENVVAR", "MyValue"));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(new[] { workingDirTestCase, environmentTestCase }, false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => CheckSingleResultHasOutcome(tr, TestOutcome.Passed))), Times.Exactly(2));
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.Outcome != TestOutcome.Passed))), Times.Never);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CMakeDisabledTest_TestIsNotRunButReportedAsSkipped()
        {
            TestCase disabledTestCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();
            TestCase environmentTestCase = TestDataCreator.GetTestCases("EnvironmentVariable.IsSet").First();
            var settings = CreateSettings(PlaceholderReplacer.SolutionDirPlaceholder, null, "MYENVVAR=MyValue");
            var disabledTest = CreateTestProperties(disabledTestCase, null);
            disabledTest.Disabled = true;
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                disabledTest, CreateTestProperties(environmentTestCase, null));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(new[] { disabledTestCase, environmentTestCase }, false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Count() == 1 && tr.Single().TestCase == disabledTestCase
                    && tr.Single().Outcome == TestOutcome.Skipped
                    && tr.Single().ErrorMessage == SequentialTestRunner.DisabledTestMessage)), Times.Once);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == disabledTestCase && result.Outcome != TestOutcome.Skipped))), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == environmentTestCase && result.Outcome == TestOutcome.Passed))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CMakeTimeout_TestIsKilledAndRemainingTestsAreRun()
        {
            TestCase timedOutTestCase = TestDataCreator.GetTestCases("LongRunningTests.Test1").First();
            TestCase otherTestCase = TestDataCreator.GetTestCases("LongRunningTests.Test2").First();
            var settings = CreateSettings(null, null);
            var timedOutTest = CreateTestProperties(timedOutTestCase, null);
            timedOutTest.Timeout = TimeSpan.FromMilliseconds(500);
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                timedOutTest, CreateTestProperties(otherTestCase, null));
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(new[] { timedOutTestCase, otherTestCase }, false, ProcessExecutorFactory);

            // the timeout result proves that the test has been killed (the duration of the run does not, since starting
            // the executable twice takes long on busy machines)
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == timedOutTestCase && result.Outcome == TestOutcome.Failed
                    && result.ErrorMessage.StartsWith(StreamingStandardOutputTestResultParser.CreateTimeoutText(TimeSpan.FromMilliseconds(500)))))), Times.Once);
            // Test2 fails, but is run and not affected by the timeout
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == otherTestCase && result.Outcome == TestOutcome.Failed
                    && !result.ErrorMessage.Contains("TIMED OUT") && !result.ErrorMessage.Contains(StreamingStandardOutputTestResultParser.CrashText)))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_TestTimeout_TestsAreKilledAndReportedAsTimedOut()
        {
            List<TestCase> testCases = TestDataCreator.GetTestCases("LongRunningTests.Test1", "LongRunningTests.Test2");
            var settings = CreateSettings(null, null, testTimeoutInSeconds: 1);
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            // the timeout results prove that the tests have been killed (see above)
            runner.RunTests(testCases, false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            foreach (TestCase testCase in testCases)
            {
                MockFrameworkReporter.Verify(r => r.ReportTestResults(
                    It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == testCase && result.Outcome == TestOutcome.Failed
                        && result.ErrorMessage.StartsWith(StreamingStandardOutputTestResultParser.CreateTimeoutText(TimeSpan.FromSeconds(1)))))), Times.Once);
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_TestTimeoutAndCMakeTimeout_CMakeTimeoutTakesPrecedence()
        {
            TestCase timedOutTestCase = TestDataCreator.GetTestCases("LongRunningTests.Test1").First();
            TestCase otherTestCase = TestDataCreator.GetTestCases("LongRunningTests.Test2").First();
            var settings = CreateSettings(null, null, testTimeoutInSeconds: 1);
            var otherTest = CreateTestProperties(otherTestCase, null);
            otherTest.Timeout = TimeSpan.FromSeconds(10);
            settings.TestPropertySettingsContainer = CreateTestPropertySettingsContainer(
                CreateTestProperties(timedOutTestCase, null), otherTest);
            var runner = new SequentialTestRunner("", 0, "", MockFrameworkReporter.Object, TestEnvironment.Logger, settings, new SchedulingAnalyzer(TestEnvironment.Logger));

            runner.RunTests(new[] { timedOutTestCase, otherTestCase }, false, ProcessExecutorFactory);

            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == timedOutTestCase && result.Outcome == TestOutcome.Failed
                    && result.ErrorMessage.StartsWith(StreamingStandardOutputTestResultParser.CreateTimeoutText(TimeSpan.FromSeconds(1)))))), Times.Once);
            // Test2 takes 2s, which is within its CMake timeout
            MockFrameworkReporter.Verify(r => r.ReportTestResults(
                It.Is<IEnumerable<TestResult>>(tr => tr.Any(result => result.TestCase == otherTestCase && result.Outcome == TestOutcome.Failed
                    && !result.ErrorMessage.Contains("TIMED OUT") && !result.ErrorMessage.Contains(StreamingStandardOutputTestResultParser.CrashText)))), Times.Once);
        }

        private static TestPropertySettingsContainer.TestProperties CreateTestProperties(TestCase testCase,
            string workingDirectory, string variableName = null, string variableValue = null)
        {
            var environment = new Dictionary<string, string>();
            if (variableName != null)
                environment.Add(variableName, variableValue);

            return new TestPropertySettingsContainer.TestProperties
            {
                Name = testCase.FullyQualifiedName,
                Command = testCase.Source,
                WorkingDirectory = workingDirectory,
                Environment = environment
            };
        }

        private static TestPropertySettingsContainer CreateTestPropertySettingsContainer(
            params TestPropertySettingsContainer.TestProperties[] tests)
        {
            return new TestPropertySettingsContainer(tests);
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

        private SettingsWrapper CreateSettings(string solutionWorkingDir, string projectWorkingDir, string environmentVariable = null,
            int? testTimeoutInSeconds = null)
        {
            var mockContainer = new Mock<IGoogleTestAdapterSettingsContainer>();

            var solutionSettings = new RunSettings {WorkingDir = solutionWorkingDir, TestTimeoutInSeconds = testTimeoutInSeconds};
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