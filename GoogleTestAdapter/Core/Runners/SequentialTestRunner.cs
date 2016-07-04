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
            string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            DebugUtils.AssertIsNotNull(userParameters, nameof(userParameters));

            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            _testEnvironment.DebugInfo($"Computed testcase groups for {groupedTestCases.Keys.Count} executables");
            TestCase[] allTestCasesAsArray = allTestCases as TestCase[] ?? allTestCases.ToArray();
            foreach (string executable in groupedTestCases.Keys)
            {
                string finalParameters = userParameters.Replace(SettingsWrapper.ExecutablePlaceholder, executable);
                if (_canceled)
                {
                    break;
                }
                RunTestsFromExecutable(
                    executable,
                    allTestCasesAsArray.Where(tc => tc.Source == executable),
                    groupedTestCases[executable],
                    baseDir,
                    finalParameters,
                    isBeingDebugged,
                    debuggedLauncher);
            }
        }

        public void Cancel()
        {
            _canceled = true;
        }


        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(string executable,
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, string userParameters,
            bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            string resultXmlFile = Path.GetTempFileName();
            string workingDir = Path.GetDirectoryName(executable);
            var serializer = new TestDurationSerializer();

            var generator = new CommandLineGenerator(allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, _testEnvironment);
            _testEnvironment.DebugInfo($"Computed {generator.GetCommandLines().Count()} arguments");
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (_canceled)
                {
                    break;
                }

                _frameworkReporter.ReportTestsStarted(arguments.TestCases);
                _testEnvironment.DebugInfo("Executing command '" + executable + " " + arguments.CommandLine + "'.");
                List<string> consoleOutput = new TestProcessLauncher(_testEnvironment, isBeingDebugged).GetOutputOfCommand(workingDir, executable, arguments.CommandLine, _testEnvironment.Options.PrintTestOutput && !_testEnvironment.Options.ParallelTestExecution, false, debuggedLauncher);
                _testEnvironment.DebugInfo("Execution finished of command '" + executable + " " + arguments.CommandLine + "'.");
                IEnumerable<TestResult> results = CollectTestResults(arguments.TestCases, resultXmlFile, consoleOutput, baseDir);
                _testEnvironment.DebugInfo($"Collected {results.Count()} test results");

                Stopwatch stopwatch = Stopwatch.StartNew();
                _frameworkReporter.ReportTestResults(results);
                stopwatch.Stop();
                _testEnvironment.DebugInfo($"Reported {results.Count()} test results to VS, executable: '{executable}', duration: {stopwatch.Elapsed}");

                serializer.UpdateTestDurations(results);
                _testEnvironment.DebugInfo($"Updated {results.Count()} test durations");
            }
        }

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput, string baseDir)
        {
            var testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            var xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, _testEnvironment, baseDir);
            var consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, _testEnvironment, baseDir);

            testResults.AddRange(xmlParser.GetTestResults());
            _testEnvironment.DebugInfo("Collected test results from XML result file");

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                List<TestResult> consoleResults = consoleParser.GetTestResults();
                // ReSharper disable once AccessToModifiedClosure
                foreach (TestResult testResult in consoleResults.Where(tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
                {
                    testResults.Add(testResult);
                }
                _testEnvironment.DebugInfo("Collected test results from console output");
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
                    errorMessage = "reason is probably a crash of test " + consoleParser.CrashedTestCase.DisplayName;
                    errorStackTrace = ErrorMessageParser.CreateStackTraceEntry("crash suspect",
                        consoleParser.CrashedTestCase.CodeFilePath, consoleParser.CrashedTestCase.LineNumber.ToString());
                }
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
                }
                _testEnvironment.DebugInfo("Created test results for tests which were neither found in xml file nor in console output");
            }

            testResults = testResults.OrderBy(tr => tr.TestCase.FullyQualifiedName).ToList();

            return testResults;
        }

    }

}