using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly TestEnvironment _testEnvironment;


        public SequentialTestRunner(ITestFrameworkReporter reporter, TestEnvironment testEnvironment)
        {
            _frameworkReporter = reporter;
            _testEnvironment = testEnvironment;
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
            }
        }

        public void Cancel()
        {
            _canceled = true;
        }


        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(string executable, string workingDir,
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, string userParameters,
            bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            string resultXmlFile = Path.GetTempFileName();
            var serializer = new TestDurationSerializer();

            var generator = new CommandLineGenerator(allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, _testEnvironment);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (_canceled)
                {
                    break;
                }
                var splitter = new TestResultSplitter(arguments.TestCases, _testEnvironment, baseDir, _frameworkReporter);
                var results = RunTests(executable, workingDir, baseDir, isBeingDebugged, debuggedLauncher, arguments, resultXmlFile, executor, splitter).ToArray();

                Stopwatch stopwatch = Stopwatch.StartNew();
                _frameworkReporter.ReportTestsStarted(results.Select(tr => tr.TestCase));
                _frameworkReporter.ReportTestResults(results);
                stopwatch.Stop();
                _testEnvironment.DebugInfo($"Reported {results.Length} test results to VS, executable: '{executable}', duration: {stopwatch.Elapsed}");

                serializer.UpdateTestDurations(results);
            }
        }

        private IEnumerable<TestResult> RunTests(string executable, string workingDir, string baseDir, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, string resultXmlFile, IProcessExecutor executor, TestResultSplitter splitter)
        {
            List<string> consoleOutput;
            if (executor != null)
            {
                string pathExtension = _testEnvironment.Options.GetPathExtension(executable);
                bool printTestOutput = _testEnvironment.Options.PrintTestOutput &&
                                       !_testEnvironment.Options.ParallelTestExecution;

                if (printTestOutput)
                    _testEnvironment.LogInfo(
                        ">>>>>>>>>>>>>>> Output of command '" + executable + " " + arguments.CommandLine + "'");

                Action<string> reportStandardOutputAction = line =>
                {
                    splitter.ReportLine(line);
                    if (printTestOutput)
                        _testEnvironment.LogInfo(line);
                };
                executor.ExecuteCommandBlocking(
                    executable, arguments.CommandLine, workingDir, pathExtension, 
                    reportStandardOutputAction, s => { });
                splitter.Flush();

                if (printTestOutput)
                    _testEnvironment.LogInfo("<<<<<<<<<<<<<<< End of Output");

                consoleOutput = new List<string>();
            }
            else
            {
                consoleOutput =
                    new TestProcessLauncher(_testEnvironment, isBeingDebugged).GetOutputOfCommand(workingDir, executable,
                        arguments.CommandLine,
                        _testEnvironment.Options.PrintTestOutput && !_testEnvironment.Options.ParallelTestExecution, false,
                        debuggedLauncher);
            }

            var remainingTestCases = 
                arguments.TestCases.Except(splitter.TestResults.Select(tr => tr.TestCase));
            IEnumerable<TestResult> results = CollectTestResults(remainingTestCases, resultXmlFile, consoleOutput, baseDir, splitter.CrashedTestCase);
            return results;
        }

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput, string baseDir, TestCase crashedTestCase)
        {
            var testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            var consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, _testEnvironment, baseDir);

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                var xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, _testEnvironment, baseDir);
                List<TestResult> xmlResults = xmlParser.GetTestResults();
                int nrOfCollectedTestResults = 0;
                // ReSharper disable once AccessToModifiedClosure
                foreach (TestResult testResult in xmlResults.Where(tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
                {
                    testResults.Add(testResult);
                    nrOfCollectedTestResults++;
                }
                _testEnvironment.DebugInfo($"Collected {nrOfCollectedTestResults} test results from XML result file");
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
                _testEnvironment.DebugInfo($"Collected {nrOfCollectedTestResults} test results from console output");
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
                _testEnvironment.DebugInfo($"Created {nrOfCreatedTestResults} test results for tests which were neither found in result XML file nor in console output");
            }

            testResults = testResults.OrderBy(tr => tr.TestCase.FullyQualifiedName).ToList();

            return testResults;
        }

    }

}