using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.TestResults;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Runners
{
    public class SequentialTestRunner : ITestRunner
    {
        private bool Canceled { get; set; } = false;

        private ITestFrameworkReporter FrameworkReporter { get; }
        private TestEnvironment TestEnvironment { get; }


        public SequentialTestRunner(ITestFrameworkReporter reporter, TestEnvironment testEnvironment)
        {
            FrameworkReporter = reporter;
            TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
            string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            DebugUtils.AssertIsNotNull(userParameters, nameof(userParameters));

            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            TestCase[] allTestCasesAsArray = allTestCases as TestCase[] ?? allTestCases.ToArray();
            foreach (string executable in groupedTestCases.Keys)
            {
                string finalParameters = userParameters.Replace(Options.ExecutablePlaceholder, executable);
                if (Canceled)
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
            Canceled = true;
        }


        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(string executable,
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, string userParameters,
            bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            string resultXmlFile = Path.GetTempFileName();
            string workingDir = Path.GetDirectoryName(executable);
            var serializer = new TestDurationSerializer();

            var generator = new CommandLineGenerator(allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, TestEnvironment);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (Canceled)
                {
                    break;
                }

                FrameworkReporter.ReportTestsStarted(arguments.TestCases);

                TestEnvironment.DebugInfo("Executing command '" + executable + " " + arguments.CommandLine + "'.");
                List<string> consoleOutput = new TestProcessLauncher(TestEnvironment, isBeingDebugged).GetOutputOfCommand(workingDir, executable, arguments.CommandLine, TestEnvironment.Options.PrintTestOutput && !TestEnvironment.Options.ParallelTestExecution, false, debuggedLauncher);
                IEnumerable<TestResult> results = CollectTestResults(arguments.TestCases, resultXmlFile, consoleOutput, baseDir);

                FrameworkReporter.ReportTestResults(results);
                serializer.UpdateTestDurations(results);
            }
        }

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput, string baseDir)
        {
            List<TestResult> testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            XmlTestResultParser xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, TestEnvironment, baseDir);
            StandardOutputTestResultParser consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, TestEnvironment, baseDir);

            testResults.AddRange(xmlParser.GetTestResults());

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                List<TestResult> consoleResults = consoleParser.GetTestResults();
                foreach (TestResult testResult in consoleResults.Where(tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
                {
                    testResults.Add(testResult);
                }
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
            }

            return testResults;
        }

    }

}