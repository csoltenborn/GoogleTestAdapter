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
        private volatile bool _canceled;
        private readonly object _cancelLock = new object();

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
                    string userParameters = _settings.GetUserParametersForExecution(executable, _testDir, _threadId);

                    var testCasesToRunOfExecutable = ReportDisabledTests(executable, groupedTestCases[executable]);

                    // tests of CMake projects might come with their own working directory and environment;
                    // tests sharing them are still run in one go
                    var testCasesByTestPropertySettings = testCasesToRunOfExecutable
                        .GroupBy(tc => _settings.GetTestPropertySettings(executable, tc.FullyQualifiedName),
                            TestPropertySettings.ExecutionEnvironmentComparer);
                    foreach (var testCases in testCasesByTestPropertySettings)
                    {
                        if (_canceled)
                            break;

                        if (testCases.Key != null)
                            _logger.DebugInfo($"{_threadName}Running {testCases.Count()} test(s) of executable '{executable}' with CMake test properties: {testCases.Key}");

                        string workingDir = _settings.GetWorkingDirForExecution(executable, _testDir, _threadId, testCases.Key);
                        IDictionary<string, string> environmentVariables = _settings.GetEnvironmentVariablesForExecution(executable, _testDir, _threadId, testCases.Key);

                        RunTestsFromExecutable(
                            executable,
                            workingDir,
                            testCases,
                            userParameters,
                            environmentVariables,
                            isBeingDebugged,
                            processExecutorFactory);
                    }
                });

            }
        }

        public const string DisabledTestMessage = "Test is disabled (CMake test property DISABLED)";

        // tests disabled in CMake are not run (as CTest does), but reported as skipped
        private List<TestCase> ReportDisabledTests(string executable, IEnumerable<TestCase> testCases)
        {
            var testCasesToRun = new List<TestCase>();
            var disabledTestResults = new List<TestResult>();
            foreach (TestCase testCase in testCases)
            {
                if (!testCase.IsExitCodeTestCase && _settings.GetTestPropertySettings(executable, testCase.FullyQualifiedName)?.Disabled == true)
                {
                    disabledTestResults.Add(new TestResult(testCase)
                    {
                        ComputerName = Environment.MachineName,
                        DisplayName = testCase.DisplayName,
                        Outcome = TestOutcome.Skipped,
                        ErrorMessage = DisabledTestMessage,
                        Duration = TimeSpan.Zero
                    });
                }
                else
                {
                    testCasesToRun.Add(testCase);
                }
            }

            if (disabledTestResults.Count > 0)
            {
                _logger.DebugInfo($"{_threadName}Not running {disabledTestResults.Count} test(s) of executable '{executable}' which are disabled in CMake");
                try
                {
                    _frameworkReporter.ReportTestsStarted(disabledTestResults.Select(tr => tr.TestCase));
                    _frameworkReporter.ReportTestResults(disabledTestResults);
                }
                catch (TestRunCanceledException e)
                {
                    _logger.DebugInfo($"{_threadName}Execution has been canceled: {e.InnerException?.Message ?? e.Message}");
                    Cancel();
                }
            }

            return testCasesToRun;
        }

        public IList<ExecutableResult> ExecutableResults { get; } = new List<ExecutableResult>();

        public void Cancel()
        {
            lock (_cancelLock)
            {
                _canceled = true;
                if (_settings.KillProcessesOnCancel)
                {
                    _processExecutor?.Cancel();
                }
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
                var streamingParser = new StreamingStandardOutputTestResultParser(arguments.TestCases, _logger, _frameworkReporter, _settings.GetSourcePathMapper(executable, _logger));
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

                // as with CTest, a test timing out does not affect the other tests
                if (streamingParser.TimedOutTestCase != null && !_canceled)
                {
                    var testCasesNotRun = GetTestCasesNotRun(arguments.TestCases, streamingParser);
                    if (testCasesNotRun.Count > 0)
                    {
                        _logger.DebugInfo($"{_threadName}Test {streamingParser.TimedOutTestCase.FullyQualifiedName} has timed out, running the remaining {testCasesNotRun.Count} test(s) of executable '{executable}'");
                        RunTestsFromExecutable(executable, workingDir, testCasesNotRun, userParameters, environmentVariables,
                            isBeingDebugged, processExecutorFactory);
                    }
                }
            }
        }

        private static List<TestCase> GetTestCasesNotRun(IEnumerable<TestCase> testCases, StreamingStandardOutputTestResultParser streamingParser)
        {
            return testCases
                .Except(streamingParser.TestResults.Select(tr => tr.TestCase))
                .Where(tc => !tc.IsExitCodeTestCase && tc != streamingParser.TimedOutTestCase)
                .ToList();
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

        public static DebuggerEngine GetDebuggerEngine(DebuggerKind debuggerKind)
        {
            switch (debuggerKind)
            {
                case DebuggerKind.Native:
                    return DebuggerEngine.Native;
                case DebuggerKind.ManagedAndNative:
                    return DebuggerEngine.ManagedAndNative;
                case DebuggerKind.ManagedCoreAndNative:
                    return DebuggerEngine.ManagedCoreAndNative;
                default:
                    throw new ArgumentOutOfRangeException(nameof(debuggerKind), debuggerKind, "Debugger kind has no debugger engine");
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
            // tests not run because of a timeout will be run again
            if (streamingParser.TimedOutTestCase != null)
                remainingTestCases = remainingTestCases.Where(tc => tc == streamingParser.TimedOutTestCase);
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
                    _logger.DebugInfo($"{_threadName}Executable {executable}: Both a path extension and a PATH environment variable have been provided, appending the path extension to the PATH environment variable");
                    environmentVariables = new Dictionary<string, string>(environmentVariables)
                    {
                        ["PATH"] = environmentVariables["PATH"] + ";" + pathExtension
                    };
                    pathExtension = null;
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

            lock (_cancelLock)
            {
                _processExecutor = isBeingDebugged
                    ? _settings.DebuggerKind == DebuggerKind.VsTestFramework
                        ? processExecutorFactory.CreateFrameworkDebuggingExecutor(printTestOutput, _logger)
                        : processExecutorFactory.CreateNativeDebuggingExecutor(
                            GetDebuggerEngine(_settings.DebuggerKind), printTestOutput, _logger)
                    : processExecutorFactory.CreateExecutor(printTestOutput, _logger);

                // execution might have been canceled after the check in RunTestsFromExecutable(), i.e., Cancel() has
                // been called on the previous executor; the executable is then not started at all
                if (_canceled)
                {
                    _processExecutor.Cancel();
                }
            }
            int exitCode;
            using (StartTimeoutWatchdog(executable, arguments.TestCases, isBeingDebugged, isTestOutputAvailable, streamingParser))
            {
                exitCode = _processExecutor.ExecuteCommandBlocking(
                    executable, arguments.CommandLine, workingDir, pathExtension, environmentVariables,
                    isTestOutputAvailable ? (Action<string>) OnNewOutputLine : null);
            }
            streamingParser.Flush(exitCode);

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

        public static readonly TimeSpan TimeoutWatchdogInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Kills the test executable if a test runs longer than its timeout as configured in CMake (test property
        /// TIMEOUT) or by option <see cref="SettingsWrapper.OptionTestTimeoutInSeconds"/>. Returns null if none of the
        /// tests has a timeout.
        /// </summary>
        private IDisposable StartTimeoutWatchdog(string executable, IEnumerable<TestCase> testCases, bool isBeingDebugged,
            bool isTestOutputAvailable, StreamingStandardOutputTestResultParser streamingParser)
        {
            // a debugged test might be paused at a breakpoint
            if (isBeingDebugged || !isTestOutputAvailable)
                return null;

            TimeSpan? defaultTimeout = _settings.TestTimeoutInSeconds > 0
                ? TimeSpan.FromSeconds(_settings.TestTimeoutInSeconds)
                : (TimeSpan?)null;
            var timeouts = new Dictionary<TestCase, TimeSpan>();
            foreach (TestCase testCase in testCases)
            {
                TimeSpan? timeout = _settings.GetTestPropertySettings(executable, testCase.FullyQualifiedName)?.Timeout
                                    ?? defaultTimeout;
                if (timeout.HasValue)
                    timeouts[testCase] = timeout.Value;
            }
            if (timeouts.Count == 0)
                return null;

            return new TimeoutWatchdog(timeouts, streamingParser, _processExecutor, testCase =>
                _logger.LogWarning($"{_threadName}Test {testCase.FullyQualifiedName} has not finished within its timeout of {timeouts[testCase].TotalSeconds}s, killing executable '{executable}'"));
        }

        private sealed class TimeoutWatchdog : IDisposable
        {
            private readonly IDictionary<TestCase, TimeSpan> _timeouts;
            private readonly StreamingStandardOutputTestResultParser _streamingParser;
            private readonly IProcessExecutor _processExecutor;
            private readonly Action<TestCase> _onTimeout;
            private readonly System.Threading.Timer _timer;
            private readonly object _lock = new object();
            private bool _isDisposed;

            public TimeoutWatchdog(IDictionary<TestCase, TimeSpan> timeouts, StreamingStandardOutputTestResultParser streamingParser,
                IProcessExecutor processExecutor, Action<TestCase> onTimeout)
            {
                _timeouts = timeouts;
                _streamingParser = streamingParser;
                _processExecutor = processExecutor;
                _onTimeout = onTimeout;
                _timer = new System.Threading.Timer(_ => Check(), null, TimeoutWatchdogInterval, TimeoutWatchdogInterval);
            }

            private void Check()
            {
                lock (_lock)
                {
                    if (_isDisposed || _streamingParser.TimedOutTestCase != null)
                        return;

                    TestCase runningTestCase = _streamingParser.GetRunningTestCase(out TimeSpan runningFor);
                    if (runningTestCase == null || !_timeouts.TryGetValue(runningTestCase, out TimeSpan timeout) || runningFor <= timeout)
                        return;

                    _onTimeout(runningTestCase);
                    _streamingParser.SetTimedOut(runningTestCase, timeout);
                    _processExecutor.Cancel();
                }
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    _isDisposed = true;
                    _timer.Dispose();
                }
            }
        }
    }

}