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


        public SequentialTestRunner(string threadName, ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
        {
            _threadName = threadName;
            _frameworkReporter = reporter;
            _logger = logger;
            _settings = settings;
            _schedulingAnalyzer = schedulingAnalyzer;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
            string workingDir, string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            DebugUtils.AssertIsNotNull(userParameters, nameof(userParameters));
            DebugUtils.AssertIsNotNull(workingDir, nameof(workingDir));

            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            TestCase[] allTestCasesAsArray = allTestCases as TestCase[] ?? allTestCases.ToArray();
            foreach (string executable in groupedTestCases.Keys)
            {
                string finalParameters = SettingsWrapper.ReplacePlaceholders(userParameters, executable);
                string finalWorkingDir = SettingsWrapper.ReplacePlaceholders(workingDir, executable);

                if (_canceled)
                    break;

                _settings.ExecuteWithSettingsForExecutable(executable, () =>
                {
                    RunTestsFromExecutable(
                        executable,
                        finalWorkingDir,
                        allTestCasesAsArray.Where(tc => tc.Source == executable),
                        groupedTestCases[executable],
                        baseDir,
                        finalParameters,
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
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, string userParameters,
            bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            string resultXmlFile = Path.GetTempFileName();
            var serializer = new TestDurationSerializer();

            var generator = new CommandLineGenerator(allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, _settings);
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
                $"{threadName}Check out Google Test Adapter's trouble shooting section at https://github.com/csoltenborn/GoogleTestAdapter#trouble_shooting");
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
            return CollectTestResults(remainingTestCases, resultXmlFile, consoleOutput, streamingParser.CrashedTestCase);
        }

        private TestProcessLauncher _processLauncher;
        private IProcessExecutor _processExecutor;

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

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput, TestCase crashedTestCase)
        {
            var testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            var consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, _logger);

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                var xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, _logger);
                List<TestResult> xmlResults = xmlParser.GetTestResults();
                int nrOfCollectedTestResults = 0;
                // ReSharper disable once AccessToModifiedClosure
                foreach (TestResult testResult in xmlResults.Where(tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
                {
                    testResults.Add(testResult);
                    nrOfCollectedTestResults++;
                }
                if (nrOfCollectedTestResults > 0)
                   _logger.DebugInfo($"{_threadName}Collected {nrOfCollectedTestResults} test results from result XML file {resultXmlFile}");
            }

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                List<TestResult> consoleResults = consoleParser.GetTestResults();
                int nrOfCollectedTestResults = 0;
                // ReSharper disable once AccessToModifiedClosure
                foreach (TestResult testResult in consoleResults.Where(tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
                {
                    testResults.Add(testResult);
                    nrOfCollectedTestResults++;
                }
                if (nrOfCollectedTestResults > 0)
                    _logger.DebugInfo($"{_threadName}Collected {nrOfCollectedTestResults} test results from console output");
            }

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                string errorMessage, errorStackTrace = null;
                if (consoleParser.CrashedTestCase == null && crashedTestCase == null)
                {
                    errorMessage = "";
                }
                else
                {
                    if (crashedTestCase == null)
                        crashedTestCase = consoleParser.CrashedTestCase;
                    errorMessage = $"reason is probably a crash of test {crashedTestCase.DisplayName}";
                    errorStackTrace = ErrorMessageParser.CreateStackTraceEntry("crash suspect",
                        crashedTestCase.CodeFilePath, crashedTestCase.LineNumber.ToString());
                }
                int nrOfCreatedTestResults = 0;
                // ReSharper disable once AccessToModifiedClosure
                foreach (TestCase testCase in testCasesRunAsArray.Where(tc => !testResults.Exists(tr => tr.TestCase.FullyQualifiedName == tc.FullyQualifiedName)))
                {
                    testResults.Add(new TestResult(testCase)
                    {
                        ComputerName = Environment.MachineName,
                        Outcome = TestOutcome.Skipped,
                        ErrorMessage = errorMessage,
                        ErrorStackTrace = errorStackTrace
                    });
                    nrOfCreatedTestResults++;
                }
                _logger.DebugInfo($"{_threadName}Created {nrOfCreatedTestResults} test results for tests which were neither found in result XML file nor in console output");
            }

            testResults = testResults.OrderBy(tr => tr.TestCase.FullyQualifiedName).ToList();

            return testResults;
        }

    }

}