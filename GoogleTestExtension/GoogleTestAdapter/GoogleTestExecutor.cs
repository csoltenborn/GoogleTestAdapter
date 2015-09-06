using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GoogleTestAdapter
{
    [ExtensionUri(EXECUTOR_URI_STRING)]
    public class GoogleTestExecutor : AbstractGoogleTestAdapterClass, ITestExecutor
    {
        public const string EXECUTOR_URI_STRING = Constants.identifierUri;
        public static readonly Uri EXECUTOR_URI = new Uri(EXECUTOR_URI_STRING);

        private bool Canceled = false;

        public GoogleTestExecutor() : this(null) { }

        public GoogleTestExecutor(IOptions options) : base(options) {}

        public void Cancel()
        {
            DebugUtils.CheckDebugModeForExecutionCode();

            Canceled = true;
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);

            Canceled = false;
            foreach (string Executable in sources)
            {
                if (Canceled)
                {
                    break;
                }

                GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer(Options);
                List<TestCase> AllCases = Discoverer.GetTestsFromExecutable(frameworkHandle, Executable);
                RunTests(true, AllCases, AllCases, runContext, frameworkHandle);
            }
        }

        public void RunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);

            Canceled = false;
            List<TestCase> AllTestCasesInAllExecutables = new List<TestCase>();
            HashSet<string> Executables = new HashSet<string>();
            TestCase[] TestCasesToRun = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            foreach (TestCase TestCase in TestCasesToRun)
            {
                Executables.Add(TestCase.Source);
            }

            foreach (string Executable in Executables)
            {
                AllTestCasesInAllExecutables.AddRange(new GoogleTestDiscoverer(Options).GetTestsFromExecutable(frameworkHandle, Executable));
            }
            RunTests(false, AllTestCasesInAllExecutables, TestCasesToRun, runContext, frameworkHandle);
        }

        public static IDictionary<string, List<TestCase>> GroupTestcasesByExecutable(IEnumerable<TestCase> testcases)
        {
            Dictionary<string, List<TestCase>> GroupedTestCases = new Dictionary<string, List<TestCase>>();
            foreach (TestCase TestCase in testcases)
            {
                List<TestCase> Group;
                if (GroupedTestCases.ContainsKey(TestCase.Source))
                {
                    Group = GroupedTestCases[TestCase.Source];
                }
                else
                {
                    Group = new List<TestCase>();
                    GroupedTestCases.Add(TestCase.Source, Group);
                }
                Group.Add(TestCase);
            }
            return GroupedTestCases;
        }

        private void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            IDictionary<string, List<TestCase>> GroupedTestCases = GroupTestcasesByExecutable(testCasesToRun);
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
            foreach(GoogleTestCommandLine.Args Arguments in new GoogleTestCommandLine(runAllTestCases, executable.Length, allTestCases, TestCasesToRun, ResultXmlFile, handle, Options).GetCommandLines())
            {
                List<string> ConsoleOutput = ProcessUtils.GetOutputOfCommand(handle, WorkingDir, executable, Arguments.CommandLine, Options.PrintTestOutput, false, runContext, handle);
                IEnumerable<TestResult> Results = CollectTestResults(ResultXmlFile, ConsoleOutput, Arguments.TestCases,
                    handle);
                Reporter.ReportTestResults(Results);
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