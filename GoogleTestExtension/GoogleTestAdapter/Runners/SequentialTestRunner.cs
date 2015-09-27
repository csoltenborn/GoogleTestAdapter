using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter.Runners
{
    class SequentialTestRunner : ITestRunner
    {
        private bool Canceled { get; set; } = false;

        private TestEnvironment TestEnvironment { get; }

        internal SequentialTestRunner(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }

        void ITestRunner.RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle)
        {
            DebugUtils.AssertIsNotNull(userParameters, nameof(userParameters));

            IDictionary<string, List<TestCase>> groupedTestCases = testCasesToRun.GroupByExecutable();
            TestCase[] allTestCasesAsArray = allTestCases as TestCase[] ?? allTestCases.ToArray();
            foreach (string executable in groupedTestCases.Keys)
            {
                if (Canceled)
                {
                    break;
                }
                RunTestsFromExecutable(executable, allTestCasesAsArray, groupedTestCases[executable], userParameters, runContext, handle);
            }
        }

        void ITestRunner.Cancel()
        {
            Canceled = true;
        }

        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(string executable,
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string userParameters,
            IRunContext runContext, IFrameworkHandle handle)
        {
            string resultXmlFile = Path.GetTempFileName();
            string workingDir = Path.GetDirectoryName(executable);
            VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(TestEnvironment);
            TestDurationSerializer serializer = new TestDurationSerializer(TestEnvironment);

            CommandLineGenerator generator = new CommandLineGenerator(allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, TestEnvironment);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (Canceled)
                {
                    break;
                }

                reporter.ReportTestsStarted(handle, arguments.TestCases);

                TestEnvironment.LogInfo("Executing command '" + executable + " " + arguments.CommandLine + "'.", TestEnvironment.LogType.UserDebug);
                List<string> consoleOutput = new ProcessLauncher(TestEnvironment).GetOutputOfCommand(workingDir, executable, arguments.CommandLine, TestEnvironment.Options.PrintTestOutput && !TestEnvironment.Options.ParallelTestExecution, false, runContext, handle);
                IEnumerable<TestResult> results = CollectTestResults(arguments.TestCases,
                    resultXmlFile, consoleOutput, handle);

                reporter.ReportTestResults(handle, results);
                serializer.UpdateTestDurations(results);
            }
        }

        private List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput, IFrameworkHandle handle)
        {
            List<TestResult> testResults = new List<TestResult>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            XmlTestResultParser xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, TestEnvironment);
            StandardOutputTestResultParser consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, TestEnvironment);

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
                foreach (TestCase testCase in testCasesRunAsArray.Where(tc => !testResults.Exists(tr => tr.TestCase.FullyQualifiedName == tc.FullyQualifiedName)))
                {
                    string errorMsg = consoleParser.CrashedTestCase == null ? ""
                        : "reason is probably a crash of test " + consoleParser.CrashedTestCase.DisplayName;
                    testResults.Add(new TestResult(testCase)
                    {
                        ComputerName = Environment.MachineName,
                        Outcome = TestOutcome.Skipped,
                        ErrorMessage = errorMsg
                    });
                }
            }

            return testResults;
        }

    }

}