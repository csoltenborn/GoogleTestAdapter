using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.TestResults;
using GoogleTestAdapter.Model;

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


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
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

        public void Cancel()
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
            TestDurationSerializer serializer = new TestDurationSerializer(TestEnvironment);

            CommandLineGenerator generator = new CommandLineGenerator(allTestCases, testCasesToRun, executable.Length, userParameters, resultXmlFile, TestEnvironment);
            foreach (CommandLineGenerator.Args arguments in generator.GetCommandLines())
            {
                if (Canceled)
                {
                    break;
                }

                FrameworkReporter.ReportTestsStarted(handle, arguments.TestCases);

                TestEnvironment.DebugInfo("Executing command '" + executable + " " + arguments.CommandLine + "'.");
                List<string> consoleOutput = new ProcessLauncher(TestEnvironment).GetOutputOfCommand(workingDir, executable, arguments.CommandLine, TestEnvironment.Options.PrintTestOutput && !TestEnvironment.Options.ParallelTestExecution, false, runContext, handle);
                IEnumerable<TestResult2> results = CollectTestResults(arguments.TestCases, resultXmlFile, consoleOutput);

                FrameworkReporter.ReportTestResults(handle, results);
                serializer.UpdateTestDurations(results);
            }
        }

        private List<TestResult2> CollectTestResults(IEnumerable<TestCase> testCasesRun, string resultXmlFile, List<string> consoleOutput)
        {
            List<TestResult2> testResults = new List<TestResult2>();

            TestCase[] testCasesRunAsArray = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            XmlTestResultParser xmlParser = new XmlTestResultParser(testCasesRunAsArray, resultXmlFile, TestEnvironment);
            StandardOutputTestResultParser consoleParser = new StandardOutputTestResultParser(testCasesRunAsArray, consoleOutput, TestEnvironment);

            testResults.AddRange(xmlParser.GetTestResults());

            if (testResults.Count < testCasesRunAsArray.Length)
            {
                List<TestResult2> consoleResults = consoleParser.GetTestResults();
                foreach (TestResult2 testResult in consoleResults.Where(tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
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
                    testResults.Add(new TestResult2(testCase)
                    {
                        ComputerName = Environment.MachineName,
                        Outcome = TestOutcome2.Skipped,
                        ErrorMessage = errorMsg
                    });
                }
            }

            return testResults;
        }

    }

}