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

                _frameworkReporter.ReportTestsStarted(arguments.TestCases);
                var results = RunTests(executable, workingDir, baseDir, isBeingDebugged, debuggedLauncher, arguments, resultXmlFile, executor).ToArray();

                Stopwatch stopwatch = Stopwatch.StartNew();
                _frameworkReporter.ReportTestResults(results);
                stopwatch.Stop();
                _testEnvironment.DebugInfo($"Reported {results.Count()} test results to VS, executable: '{executable}', duration: {stopwatch.Elapsed}");

                serializer.UpdateTestDurations(results);
            }
        }

        private IEnumerable<TestResult> RunTests(string executable, string workingDir, string baseDir, bool isBeingDebugged,
            IDebuggedProcessLauncher debuggedLauncher, CommandLineGenerator.Args arguments, string resultXmlFile, IProcessExecutor executor)
        {
            List<string> consoleOutput;
            if (executor != null)
            {
                consoleOutput = new List<string>();
                executor.ExecuteCommandBlocking(executable, arguments.CommandLine, workingDir, s => consoleOutput.Add(s),
                    s => { });
            }
            else
            {
                consoleOutput =
                    new TestProcessLauncher(_testEnvironment, isBeingDebugged).GetOutputOfCommand(workingDir, executable,
                        arguments.CommandLine,
                        _testEnvironment.Options.PrintTestOutput && !_testEnvironment.Options.ParallelTestExecution, false,
                        debuggedLauncher);
            }
            IEnumerable<TestResult> results = CollectTestResults(arguments.TestCases, resultXmlFile, consoleOutput, baseDir);
            return results;
        }

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput, string baseDir)
        {
            var testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            var xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, _testEnvironment, baseDir);
            var consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, _testEnvironment, baseDir);

            testResults.AddRange(xmlParser.GetTestResults());
            _testEnvironment.DebugInfo($"Collected {testResults.Count} test results from XML result file '{resultXmlFile}'");

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
                if (consoleParser.CrashedTestCase == null)
                {
                    errorMessage = "";
                }
                else
                {
                    errorMessage = $"reason is probably a crash of test {consoleParser.CrashedTestCase.DisplayName}";
                    errorStackTrace = ErrorMessageParser.CreateStackTraceEntry("crash suspect",
                        consoleParser.CrashedTestCase.CodeFilePath, consoleParser.CrashedTestCase.LineNumber.ToString());
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