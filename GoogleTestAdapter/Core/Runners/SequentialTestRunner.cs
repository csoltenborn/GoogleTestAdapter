// This file has been modified by Microsoft on 6/2017.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.TestResults;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Runners
{
    public class SequentialTestRunner : ITestRunner
    {
        private bool _canceled;

        private readonly string _threadName;
        private readonly int _threadId;
        private readonly string _testDir;
        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private TestProcessLauncher _processLauncher;
        private IProcessExecutor _processExecutor;

        public SequentialTestRunner(string threadName, int threadId, string testDir, ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
        {
            _threadName = threadName;
            _threadId = threadId;
            _testDir = testDir;
            _frameworkReporter = reporter;
            _logger = logger;
            _settings = settings;
            _schedulingAnalyzer = schedulingAnalyzer;
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            foreach (string executable in groupedTestCases.Keys)
            {
                if (_canceled)
                    break;

                _settings.ExecuteWithSettingsForExecutable(executable, () =>
                {
                    string workingDir = _settings.GetWorkingDirForExecution(executable, _testDir, _threadId);
                    string userParameters = _settings.GetUserParametersForExecution(executable, _testDir, _threadId);

                    RunTestsFromExecutable(
                        executable,
                        workingDir,
                        groupedTestCases[executable],
                        userParameters,
                        isBeingDebugged,
                        debuggedLauncher,
                        executor);
                }, _logger);

            }
        }

        public void Cancel()
        {
            _canceled = true;
            if (_settings.KillProcessesOnCancel)
            {
                _processLauncher?.Cancel();
                _processExecutor?.Cancel();
            }
        }


        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(string executable, string workingDir,
            IEnumerable<TestCase> testCasesToRun, string userParameters,
            bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            string resultXmlFile = Path.GetTempFileName();
            var serializer = new TestDurationSerializer();

            var generator = new CommandLineGenerator(testCasesToRun, executable.Length, userParameters, resultXmlFile, _settings);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (_canceled)
                {
                    break;
                }
                var streamingParser = new StreamingStandardOutputTestResultParser(arguments.TestCases, _logger, _frameworkReporter);
                var results = RunTests(executable, workingDir, isBeingDebugged, debuggedLauncher, arguments, resultXmlFile, executor, streamingParser).ToArray();

                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    _frameworkReporter.ReportTestsStarted(results.Select(tr => tr.TestCase));
                    _frameworkReporter.ReportTestResults(results);
                    stopwatch.Stop();
                    if (results.Length > 0)
                        _logger.DebugInfo($"{_threadName}Reported {results.Length} test results to VS, executable: '{executable}', duration: {stopwatch.Elapsed}");
                }
                catch (TestRunCanceledException e)
                {
                    _logger.DebugInfo($"{_threadName}Execution has been canceled: {e.InnerException?.Message ?? e.Message}");
                    Cancel();
                }

                serializer.UpdateTestDurations(results);
                foreach (TestResult result in results)
                {
                    if (!_schedulingAnalyzer.AddActualDuration(result.TestCase, (int)result.Duration.TotalMilliseconds))
                        _logger.DebugWarning("TestCase already in analyzer: " + result.TestCase.FullyQualifiedName);
                }
            }
        }

        private IEnumerable<TestResult> RunTests(string executable, string workingDir, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, string resultXmlFile, IProcessExecutor executor, StreamingStandardOutputTestResultParser streamingParser)
        {
            try
            {
                return TryRunTests(executable, workingDir, isBeingDebugged, debuggedLauncher, arguments, resultXmlFile, executor, streamingParser);
            }
            catch (Exception e)
            {
                LogExecutionError(_logger, executable, workingDir, arguments.CommandLine, e);
                return new TestResult[0];
            }
        }

        public static void LogExecutionError(ILogger logger, string executable, string workingDir, string arguments, Exception exception, string threadName = "")
        {
            logger.LogError($"{threadName}Failed to run test executable '{executable}': {exception.Message}");
            logger.DebugError($@"{threadName}Stacktrace:{Environment.NewLine}{exception.StackTrace}");
            logger.LogError(
                $"{threadName}{Strings.Instance.TroubleShootingLink}");
            logger.LogError(
                $"{threadName}In particular: launch command prompt, change into directory '{workingDir}', and execute the following command to make sure your tests can be run in general.{Environment.NewLine}{executable} {arguments}");
        }

        private IEnumerable<TestResult> TryRunTests(string executable, string workingDir, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, string resultXmlFile, IProcessExecutor executor,
            StreamingStandardOutputTestResultParser streamingParser)
        {
            List<string> consoleOutput;
            if (_settings.UseNewTestExecutionFramework)
            {
                DebugUtils.AssertIsNotNull(executor, nameof(executor));
                consoleOutput = RunTestExecutableWithNewFramework(executable, workingDir, arguments, executor, streamingParser);
            }
            else
            {
                _processLauncher = new TestProcessLauncher(_logger, _settings, isBeingDebugged);
                consoleOutput =
                    _processLauncher.GetOutputOfCommand(workingDir, executable, arguments.CommandLine,
                            _settings.PrintTestOutput && !_settings.ParallelTestExecution, false,
                            debuggedLauncher);
            }

            var remainingTestCases =
                arguments.TestCases.Except(streamingParser.TestResults.Select(tr => tr.TestCase));
            var testResults = new TestResultCollector(_logger, _threadName)
                .CollectTestResults(remainingTestCases, resultXmlFile, consoleOutput, streamingParser.CrashedTestCase);
            testResults = testResults.OrderBy(tr => tr.TestCase.FullyQualifiedName).ToList();

            return testResults;
        }

        private List<string> RunTestExecutableWithNewFramework(string executable, string workingDir, CommandLineGenerator.Args arguments, IProcessExecutor executor,
            StreamingStandardOutputTestResultParser streamingParser)
        {
            string pathExtension = _settings.GetPathExtension(executable);
            bool printTestOutput = _settings.PrintTestOutput &&
                                   !_settings.ParallelTestExecution;

            if (printTestOutput)
                _logger.LogInfo(
                    $"{_threadName}>>>>>>>>>>>>>>> Output of command '" + executable + " " + arguments.CommandLine + "'");

            Action<string> reportOutputAction = line =>
            {
                try
                {
                    if (!_canceled)
                        streamingParser.ReportLine(line);

                    if (printTestOutput)
                        _logger.LogInfo(line);
                }
                catch (TestRunCanceledException e)
                {
                    _logger.DebugInfo($"{_threadName}Execution has been canceled: {e.InnerException?.Message ?? e.Message}");
                    Cancel();
                }
            };
            _processExecutor = executor;
            _processExecutor.ExecuteCommandBlocking(
                executable, arguments.CommandLine, workingDir, pathExtension,
                reportOutputAction);
            streamingParser.Flush();

            if (printTestOutput)
                _logger.LogInfo($"{_threadName}<<<<<<<<<<<<<<< End of Output");

            var consoleOutput = new List<string>();
            new TestDurationSerializer().UpdateTestDurations(streamingParser.TestResults);
            _logger.DebugInfo(
                $"{_threadName}Reported {streamingParser.TestResults.Count} test results to VS during test execution, executable: '{executable}'");
            foreach (TestResult result in streamingParser.TestResults)
            {
                if (!_schedulingAnalyzer.AddActualDuration(result.TestCase, (int) result.Duration.TotalMilliseconds))
                    _logger.LogWarning($"{_threadName}TestCase already in analyzer: {result.TestCase.FullyQualifiedName}");
            }
            return consoleOutput;
        }
    }

}