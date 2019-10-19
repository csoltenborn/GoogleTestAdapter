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
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
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


        public void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, IDebuggedProcessExecutorFactory processExecutorFactory)
        {
            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            foreach (string executable in groupedTestCases.Keys)
            {
                if (_canceled)
                    break;

                _settings.ExecuteWithSettingsForExecutable(executable, _logger, () =>
                {
                    string workingDir = _settings.GetWorkingDirForExecution(executable, _testDir, _threadId);
                    string userParameters = _settings.GetUserParametersForExecution(executable, _testDir, _threadId);
                    IDictionary<string, string> environmentVariables = _settings.GetEnvironmentVariablesForExecution(executable, _testDir, _threadId);

                    RunTestsFromExecutable(
                        executable,
                        workingDir,
                        groupedTestCases[executable],
                        userParameters,
                        environmentVariables,
                        isBeingDebugged,
                        processExecutorFactory);
                });

            }
        }

        public IList<ExecutableResult> ExecutableResults { get; } = new List<ExecutableResult>();

        public void Cancel()
        {
            _canceled = true;
            if (_settings.KillProcessesOnCancel)
            {
                _processExecutor?.Cancel();
            }
        }


        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(string executable, string workingDir,
            IEnumerable<TestCase> testCasesToRun, string userParameters, IDictionary<string, string> environmentVariables,
            bool isBeingDebugged, IDebuggedProcessExecutorFactory processExecutorFactory)
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
                var results = RunTests(executable, workingDir, isBeingDebugged, processExecutorFactory, arguments, environmentVariables, resultXmlFile, streamingParser).ToArray();

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
            IDebuggedProcessExecutorFactory processExecutorFactory, CommandLineGenerator.Args arguments, IDictionary<string, string> environmentVariables, string resultXmlFile, StreamingStandardOutputTestResultParser streamingParser)
        {
            try
            {
                return TryRunTests(executable, workingDir, isBeingDebugged, processExecutorFactory, arguments, environmentVariables, resultXmlFile, streamingParser);
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
            if (exception is AggregateException aggregateException)
            {
               exception = aggregateException.Flatten();
            }
            logger.DebugError($@"{threadName}Exception:{Environment.NewLine}{exception}");
            logger.LogError(
                $"{threadName}{Strings.Instance.TroubleShootingLink}");
            logger.LogError(
                $"{threadName}In particular: launch command prompt, change into directory '{workingDir}', and execute the following command to make sure your tests can be run in general.{Environment.NewLine}{executable} {arguments}");
        }

        private IEnumerable<TestResult> TryRunTests(string executable, string workingDir, bool isBeingDebugged,
            IDebuggedProcessExecutorFactory processExecutorFactory, CommandLineGenerator.Args arguments, IDictionary<string, string> environmentVariables, string resultXmlFile,
            StreamingStandardOutputTestResultParser streamingParser)
        {
            var consoleOutput = 
                RunTestExecutable(executable, workingDir, arguments, environmentVariables, isBeingDebugged, processExecutorFactory, streamingParser);

            var remainingTestCases =
                arguments.TestCases
                    .Except(streamingParser.TestResults.Select(tr => tr.TestCase))
                    .Where(tc => !tc.IsExitCodeTestCase);
            var testResults = new TestResultCollector(_logger, _threadName, _settings)
                .CollectTestResults(remainingTestCases, executable, resultXmlFile, consoleOutput, streamingParser.CrashedTestCase);
            testResults = testResults.OrderBy(tr => tr.TestCase.FullyQualifiedName).ToList();

            return testResults;
        }

        private List<string> RunTestExecutable(string executable, string workingDir, CommandLineGenerator.Args arguments, IDictionary<string, string> environmentVariables, bool isBeingDebugged, IDebuggedProcessExecutorFactory processExecutorFactory,
            StreamingStandardOutputTestResultParser streamingParser)
        {
            string pathExtension = _settings.GetPathExtension(executable);
            if (!string.IsNullOrEmpty(pathExtension))
            {
                if (environmentVariables.ContainsKey("PATH") && !string.IsNullOrEmpty(environmentVariables["PATH"]))
                {
                    _logger.LogWarning($"Executable {executable}: Both a path extension and a PATH environment variable have been provided! The PATH environment variable will be ignored.");
                    environmentVariables.Remove("PATH");
                }
            }

            bool isTestOutputAvailable = !isBeingDebugged || _settings.DebuggerKind > DebuggerKind.VsTestFramework;
            bool printTestOutput = _settings.PrintTestOutput &&
                                   !_settings.ParallelTestExecution &&
                                   isTestOutputAvailable;

            void OnNewOutputLine(string line)
            {
                try
                {
                    if (!_canceled) streamingParser.ReportLine(line);
                }
                catch (TestRunCanceledException e)
                {
                    _logger.DebugInfo($"{_threadName}Execution has been canceled: {e.InnerException?.Message ?? e.Message}");
                    Cancel();
                }
            }

            _processExecutor = isBeingDebugged
                ? _settings.DebuggerKind == DebuggerKind.VsTestFramework
                    ? processExecutorFactory.CreateFrameworkDebuggingExecutor(printTestOutput, _logger)
                    : processExecutorFactory.CreateNativeDebuggingExecutor(
                        _settings.DebuggerKind == DebuggerKind.Native ? DebuggerEngine.Native : DebuggerEngine.ManagedAndNative, 
                        printTestOutput, _logger)
                : processExecutorFactory.CreateExecutor(printTestOutput, _logger);
            int exitCode = _processExecutor.ExecuteCommandBlocking(
                executable, arguments.CommandLine, workingDir, pathExtension, environmentVariables,
                isTestOutputAvailable ? (Action<string>) OnNewOutputLine : null);
            streamingParser.Flush();

            ExecutableResults.Add(new ExecutableResult(executable, exitCode, streamingParser.ExitCodeOutput,
                streamingParser.ExitCodeSkip));

            var consoleOutput = new List<string>();
            new TestDurationSerializer().UpdateTestDurations(streamingParser.TestResults);
            _logger.DebugInfo(
                $"{_threadName}Reported {streamingParser.TestResults.Count} test results to VS during test execution, executable: '{executable}'");
            foreach (TestResult result in streamingParser.TestResults)
            {
                if (!_schedulingAnalyzer.AddActualDuration(result.TestCase, (int) result.Duration.TotalMilliseconds))
                    _logger.DebugWarning($"{_threadName}TestCase already in analyzer: {result.TestCase.FullyQualifiedName}");
            }
            return consoleOutput;
        }
    }

}