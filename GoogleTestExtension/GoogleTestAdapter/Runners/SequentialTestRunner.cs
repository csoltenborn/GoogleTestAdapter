using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.TestResults;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Runners
{
    class SequentialTestRunner : AbstractOptionsProvider, ITestRunner
    {
        private bool Canceled { get; set; } = false;

        internal SequentialTestRunner(AbstractOptions options) : base(options) { }

        void ITestRunner.RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
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
                RunTestsFromExecutable(runAllTestCases, executable, allTestCasesAsArray, groupedTestCases[executable], userParameters, runContext, handle);
            }
        }

        void ITestRunner.Cancel()
        {
            Canceled = true;
        }

        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(bool runAllTestCases, string executable,
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string userParameters,
            IRunContext runContext, IFrameworkHandle handle)
        {
            string resultXmlFile = Path.GetTempFileName();
            string workingDir = Path.GetDirectoryName(executable);
            VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(Options, handle);
            TestDurationSerializer serializer = new TestDurationSerializer(Options);

            CommandLineGenerator generator = new CommandLineGenerator(runAllTestCases, allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, handle, Options);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (Canceled)
                {
                    break;
                }

                reporter.ReportTestsStarted(handle, arguments.TestCases);

                DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Executing command '" + executable + " " + arguments.CommandLine + "'.");
                List<string> consoleOutput = new ProcessLauncher(Options).GetOutputOfCommand(handle, workingDir, executable, arguments.CommandLine, Options.PrintTestOutput && !Options.ParallelTestExecution, false, runContext, handle);
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
            XmlTestResultParser xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, handle, Options);
            StandardOutputTestResultParser consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, handle, Options);

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