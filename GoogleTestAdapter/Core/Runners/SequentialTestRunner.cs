// This file has been modified by Microsoft on 11/2022.

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
        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private TestProcessLauncher _processLauncher;
        private IProcessExecutor _processExecutor;

        public SequentialTestRunner(string threadName, ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
        {
            _threadName = threadName;
            _frameworkReporter = reporter;
            _logger = logger;
            _settings = settings;
            _schedulingAnalyzer = schedulingAnalyzer;
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, string baseDir,
            string workingDir, string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            DebugUtils.AssertIsNotNull(userParameters, nameof(userParameters));
            DebugUtils.AssertIsNotNull(workingDir, nameof(workingDir));

            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            foreach (string executable in groupedTestCases.Keys)
            {
                string finalParameters = SettingsWrapper.ReplacePlaceholders(userParameters, executable);
                string finalWorkingDir = SettingsWrapper.ReplacePlaceholders(workingDir, executable);

                if (_canceled)
                    break;

                _settings.ExecuteWithSettingsForExecutable(executable, () =>
                {
                    var testsWithNoTestPropertySettings = groupedTestCases[executable];

                    if (_settings.TestPropertySettingsContainer != null)
                    {
                        testsWithNoTestPropertySettings = new List<TestCase>();

                        foreach (var testCase in groupedTestCases[executable])
                        {
                            var key = Path.GetFullPath(testCase.Source) + ":" + testCase.FullyQualifiedName;
                            ITestPropertySettings settings;
                            // Tests with default settings are treated as not having settings and can be run together
                            if (_settings.TestPropertySettingsContainer.TryGetSettings(key, out settings)
                                && (settings.Environment.Count > 0 || Path.GetFullPath(settings.WorkingDirectory) != Path.GetFullPath(finalWorkingDir)))
                            {
                                RunTestsFromExecutable(
                                    executable,
                                    settings.WorkingDirectory,
                                    settings.Environment,
                                    Enumerable.Repeat(testCase, 1),
                                    finalParameters,
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
                            finalWorkingDir,
                            null,
                            testsWithNoTestPropertySettings,
                            finalParameters,
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
            var serializer = new TestDurationSerializer();

            var generator = new CommandLineGenerator(testCasesToRun, executable.Length, userParameters, _settings);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (_canceled)
                {
                    break;
                }
                var streamingParser = new StreamingStandardOutputTestResultParser(arguments.TestCases, _logger, _frameworkReporter);
                var results = RunTests(executable, workingDir, envVars, isBeingDebugged, debuggedLauncher, arguments, executor, streamingParser).ToArray();

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
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, IProcessExecutor executor, StreamingStandardOutputTestResultParser streamingParser)
        {
            try
            {
                return TryRunTests(executable, workingDir, envVars, isBeingDebugged, debuggedLauncher, arguments, executor, streamingParser);
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
            logger.DebugError(String.Format(Resources.StackTrace, threadName, Environment.NewLine, exception.StackTrace));
            logger.LogError(String.Format(CommonResources.TroubleShootingLink, threadName));
            logger.LogError(String.Format(Resources.ExecuteSteps, threadName, workingDir, Environment.NewLine, executable, arguments));
        }

        private IEnumerable<TestResult> TryRunTests(string executable, string workingDir, IDictionary<string, string> envVars, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, IProcessExecutor executor, StreamingStandardOutputTestResultParser streamingParser)
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
                .CollectTestResults(remainingTestCases, consoleOutput, streamingParser.CrashedTestCase);
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