// This file has been modified by Microsoft on 5/2018.

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

                    var testsWithNoTestPropertySettings = groupedTestCases[executable];

                    if (_settings.TestPropertySettingsContainer != null)
                    {
                        testsWithNoTestPropertySettings = new List<TestCase>();

                        foreach (var testCase in groupedTestCases[executable])
                        {
                            ITestPropertySettings settings;
                            if (_settings.TestPropertySettingsContainer.TryGetSettings(testCase.FullyQualifiedName, out settings))
                            {
                                RunTestsFromExecutable(
                                    executable,
                                    workingDir,
                                    settings.Environment,
                                    Enumerable.Repeat(testCase, 1), // TODO this appears to be highly inefficient. Why not collect them and run them alltogether? If environments might differ, we should still group them accordingly.
                                    userParameters,
                                    isBeingDebugged,
                                    debuggedLauncher,
                                    executor);
                            }
                            else
                            {
                                testsWithNoTestPropertySettings.Add(testCase);
                            }
                        }
                    }

                    if (testsWithNoTestPropertySettings.Count != 0)
                    {
                        RunTestsFromExecutable(
                            executable,
                            workingDir,
                            null,
                            testsWithNoTestPropertySettings,
                            userParameters,
                            isBeingDebugged,
                            debuggedLauncher,
                            executor);
                    }
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
        private void RunTestsFromExecutable(string executable, string workingDir, IDictionary<string, string> envVars,
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
                var results = RunTests(executable, workingDir, envVars, isBeingDebugged, debuggedLauncher, arguments, resultXmlFile, executor, streamingParser).ToArray();

                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    _frameworkReporter.ReportTestsStarted(results.Select(tr => tr.TestCase));
                    _frameworkReporter.ReportTestResults(results);
                    stopwatch.Stop();
                    if (results.Length > 0)
                        _logger.DebugInfo(String.Format(Resources.ReportedTestResults, _threadName, results.Length, executable, stopwatch.Elapsed));
                }
                catch (TestRunCanceledException e)
                {
                    _logger.DebugInfo(String.Format(Resources.ExecutionCancelled, _threadName, e.InnerException?.Message ?? e.Message));
                    Cancel();
                }

                serializer.UpdateTestDurations(results);
                foreach (TestResult result in results)
                {
                    if (!_schedulingAnalyzer.AddActualDuration(result.TestCase, (int)result.Duration.TotalMilliseconds))
                        _logger.DebugWarning(String.Format(Resources.TestCaseInAnalyzer, result.TestCase.FullyQualifiedName));
                }
            }
        }

        private IEnumerable<TestResult> RunTests(string executable, string workingDir, IDictionary<string, string> envVars, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, string resultXmlFile, IProcessExecutor executor, StreamingStandardOutputTestResultParser streamingParser)
        {
            try
            {
                return TryRunTests(executable, workingDir, envVars, isBeingDebugged, debuggedLauncher, arguments, resultXmlFile, executor, streamingParser);
            }
            catch (Exception e)
            {
                LogExecutionError(_logger, executable, workingDir, arguments.CommandLine, e);
                return new TestResult[0];
            }
        }

        public static void LogExecutionError(ILogger logger, string executable, string workingDir, string arguments, Exception exception, string threadName = "")
        {
            logger.LogError(String.Format(Resources.RunExecutableError, threadName, executable, exception.Message));
            if (exception is AggregateException aggregateException)
            {
               exception = aggregateException.Flatten();
            }
            logger.DebugError($@"{threadName}Exception:{Environment.NewLine}{exception}");
            logger.LogError(String.Format(Common.Resources.TroubleShootingLink, threadName));
            logger.LogError(String.Format(Resources.ExecuteSteps, threadName, workingDir, Environment.NewLine, executable, arguments));
        }

        private IEnumerable<TestResult> TryRunTests(string executable, string workingDir, IDictionary<string, string> envVars, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, string resultXmlFile, IProcessExecutor executor,
            StreamingStandardOutputTestResultParser streamingParser)
        {
            List<string> consoleOutput;
            if (_settings.UseNewTestExecutionFramework)
            {
                DebugUtils.AssertIsNotNull(executor, nameof(executor));
                consoleOutput = RunTestExecutableWithNewFramework(executable, workingDir, envVars, arguments, executor, streamingParser);
            }
            else
            {
                _processLauncher = new TestProcessLauncher(_logger, _settings, isBeingDebugged);
                consoleOutput =
                    _processLauncher.GetOutputOfCommand(workingDir, envVars, executable, arguments.CommandLine,
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

        private List<string> RunTestExecutableWithNewFramework(string executable, string workingDir, IDictionary<string, string> envVars, 
            CommandLineGenerator.Args arguments, IProcessExecutor executor,
            StreamingStandardOutputTestResultParser streamingParser)
        {
            string pathExtension = _settings.GetPathExtension(executable);
            bool printTestOutput = _settings.PrintTestOutput &&
                                   !_settings.ParallelTestExecution;

            if (printTestOutput)
                _logger.LogInfo(String.Format(Resources.OutputOfCommandMessage, _threadName, executable, arguments.CommandLine));

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
                    _logger.DebugInfo(String.Format(Resources.ExecutionCancelled, _threadName, e.InnerException?.Message ?? e.Message));
                    Cancel();
                }
            };
            _processExecutor = executor;
            _processExecutor.ExecuteCommandBlocking(
                executable, arguments.CommandLine, workingDir, envVars, pathExtension,
                reportOutputAction);
            streamingParser.Flush();

            if (printTestOutput)
                _logger.LogInfo(String.Format(Resources.EndOfOutputMessage, _threadName));

            var consoleOutput = new List<string>();
            new TestDurationSerializer().UpdateTestDurations(streamingParser.TestResults);
            _logger.DebugInfo(String.Format(Resources.ReportedResultsToVS, _threadName, streamingParser.TestResults.Count, executable));

            foreach (TestResult result in streamingParser.TestResults)
            {
                if (!_schedulingAnalyzer.AddActualDuration(result.TestCase, (int) result.Duration.TotalMilliseconds))
                    _logger.LogWarning(String.Format(Resources.AlreadyInAnalyzer, _threadName, result.TestCase.FullyQualifiedName));
            }
            return consoleOutput;
        }
    }

}