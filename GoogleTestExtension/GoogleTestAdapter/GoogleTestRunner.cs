using GoogleTestAdapter.Scheduling;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleTestAdapter
{
    public class GoogleTestRunner : AbstractGoogleTestAdapterClass, IGoogleTestRunner
    {
        public bool Canceled { get; set; } = false;

        public GoogleTestRunner(IOptions options) : base(options) { }

        public void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            IDictionary<string, List<TestCase>> GroupedTestCases = GoogleTestExecutor.GroupTestcasesByExecutable(testCasesToRun);
            TestCase[] AllTestCases = allTestCases.ToArray();
            foreach (string Executable in GroupedTestCases.Keys)
            {
                if (Canceled)
                {
                    break;
                }
                try
                {
                    RunTestsFromExecutable(runAllTestCases, Executable, AllTestCases, GroupedTestCases[Executable], runContext, handle);
                }
                catch (Exception e)
                {
                    handle.SendMessage(TestMessageLevel.Error, e.Message);
                    handle.SendMessage(TestMessageLevel.Error, e.StackTrace);
                }
            }

        }

        // ReSharper disable once UnusedParameter.Local
        private void RunTestsFromExecutable(bool runAllTestCases, string executable, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] TestCasesToRun = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            foreach (TestCase TestCase in TestCasesToRun)
            {
                handle.RecordStart(TestCase);
            }

            string ResultXmlFile = Path.GetTempFileName();
            string WorkingDir = Path.GetDirectoryName(executable);
            TestResultReporter Reporter = new TestResultReporter(handle);
            foreach (GoogleTestCommandLine.Args Arguments in new GoogleTestCommandLine(runAllTestCases, executable.Length, allTestCases, TestCasesToRun, ResultXmlFile, handle, Options).GetCommandLines())
            {
                List<string> ConsoleOutput = ProcessUtils.GetOutputOfCommand(handle, WorkingDir, executable, Arguments.CommandLine, Options.PrintTestOutput && !Options.ParallelTestExecution, false, runContext, handle);
                IEnumerable<TestResult> Results = CollectTestResults(ResultXmlFile, ConsoleOutput, Arguments.TestCases,
                    handle);
                Reporter.ReportTestResults(Results);

                TestDurationSerializer serializer = new TestDurationSerializer();
                serializer.UpdateTestDurations(Results);
            }
        }

        private List<TestResult> CollectTestResults(string resultXmlFile, List<string> consoleOutput, IEnumerable<TestCase> testCasesRun, IFrameworkHandle handle)
        {
            List<TestResult> TestResults = new List<TestResult>();

            TestCase[] TestCasesRun = testCasesRun as TestCase[] ?? testCasesRun.ToArray();
            GoogleTestResultXmlParser XmlParser = new GoogleTestResultXmlParser(resultXmlFile, TestCasesRun, handle);
            TestResults.AddRange(XmlParser.GetTestResults());

            if (TestResults.Count < TestCasesRun.Length)
            {
                GoogleTestResultStandardOutputParser ConsoleParser = new GoogleTestResultStandardOutputParser(consoleOutput, TestCasesRun, handle);
                List<TestResult> ConsoleResults = ConsoleParser.GetTestResults();
                foreach (TestResult TestResult in ConsoleResults.Where(TR => !TestResults.Exists(TR2 => TR.TestCase.FullyQualifiedName == TR2.TestCase.FullyQualifiedName)))
                {
                    TestResults.Add(TestResult);
                }

                if (TestResults.Count < TestCasesRun.Length)
                {
                    foreach (TestCase TestCase in TestCasesRun.Where(TC => !TestResults.Exists(TR => TR.TestCase.FullyQualifiedName == TC.FullyQualifiedName)))
                    {
                        string ErrorMsg = ConsoleParser.CrashedTestCase == null ? ""
                            : "reason is probably a crash of test " + ConsoleParser.CrashedTestCase.DisplayName;
                        TestResults.Add(new TestResult(TestCase)
                        {
                            ComputerName = Environment.MachineName,
                            Outcome = TestOutcome.Skipped,
                            ErrorMessage = ErrorMsg
                        });
                    }
                }
            }

            return TestResults;
        }

    }
}
