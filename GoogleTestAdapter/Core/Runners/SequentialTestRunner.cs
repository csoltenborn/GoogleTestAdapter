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
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly ProcessExecutor _processExecutor;
        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;


        public SequentialTestRunner(string threadName, IDebuggerAttacher debuggerAttacher, ITestFrameworkReporter reporter, SchedulingAnalyzer schedulingAnalyzer, SettingsWrapper settings, ILogger logger)
        {
            _threadName = threadName;
            _frameworkReporter = reporter;
            _logger = logger;
            _settings = settings;
            _schedulingAnalyzer = schedulingAnalyzer;
            _processExecutor = new ProcessExecutor(debuggerAttacher, logger);
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
            string workingDir, string userParameters)
        {
            Utils.AssertIsNotNull(userParameters, nameof(userParameters));
            Utils.AssertIsNotNull(workingDir, nameof(workingDir));

            IDictionary<string, List<TestCase>> groupedTestCases = TestCase.GroupByExecutable(testCasesToRun);
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
                        finalParameters);
                }, _logger);

            }
        }

        public void Cancel()
        {
            _canceled = true;
            if (_settings.KillProcessesOnCancel)
            {
                _processExecutor?.Cancel();
            }
        }


        private void RunTestsFromExecutable(string executable, string workingDir,
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, string userParameters)
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
                var streamingParser = new StreamingTestOutputParser(arguments.TestCases, _logger, baseDir, _frameworkReporter);
                var results = RunTests(executable, workingDir, baseDir, arguments, resultXmlFile, streamingParser).ToArray();

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

        private IEnumerable<TestResult> RunTests(string executable, string workingDir, string baseDir,
            CommandLineGenerator.Args arguments, string resultXmlFile, StreamingTestOutputParser streamingParser)
        {
            try
            {
                return TryRunTests(executable, workingDir, baseDir, arguments, resultXmlFile, streamingParser);
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

        private IEnumerable<TestResult> TryRunTests(string executable, string workingDir, string baseDir,
            CommandLineGenerator.Args arguments, string resultXmlFile, 
            StreamingTestOutputParser streamingParser)
        {
            RunTestExecutable(executable, workingDir, arguments, streamingParser);

            var remainingTestCases =
                arguments.TestCases.Except(streamingParser.TestResults.Select(tr => tr.TestCase));
            return CollectTestResults(remainingTestCases, resultXmlFile, baseDir, streamingParser.CrashedTestCase);
        }

        private void RunTestExecutable(string executable, string workingDir, CommandLineGenerator.Args arguments,
            StreamingTestOutputParser streamingParser)
        {
            string pathExtension = _settings.GetPathExtension(executable);
            bool printTestOutput = _settings.PrintTestOutput &&
                                   !_settings.ParallelTestExecution;

            if (printTestOutput)
                _logger.LogInfo(
                    $"{_threadName}>>>>>>>>>>>>>>> Output of command '{executable} {arguments.CommandLine}'");

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
            _processExecutor.ExecuteCommandBlocking(
                executable, arguments.CommandLine, workingDir, pathExtension,
                reportOutputAction);
            streamingParser.Flush();

            if (printTestOutput)
                _logger.LogInfo($"{_threadName}<<<<<<<<<<<<<<< End of Output");

            new TestDurationSerializer().UpdateTestDurations(streamingParser.TestResults);
            _logger.DebugInfo(
                $"{_threadName}Reported {streamingParser.TestResults.Count} test results to VS during test execution, executable: '{executable}'");
            foreach (TestResult result in streamingParser.TestResults)
            {
                if (!_schedulingAnalyzer.AddActualDuration(result.TestCase, (int) result.Duration.TotalMilliseconds))
                    _logger.LogWarning($"{_threadName}TestCase already in analyzer: {result.TestCase.FullyQualifiedName}");
            }
        }

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, string baseDir, TestCase crashedTestCase)
        {
            var testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                var xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, _logger, baseDir);
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
                string errorMessage, errorStackTrace = null;
                if (crashedTestCase == null)
                {
                    errorMessage = "";
                }
                else
                {
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