using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

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
            List<TestCase> AllTestCasesInAllExecutables = new List<TestCase>();
            GoogleTestDiscoverer Discoverer = new GoogleTestDiscoverer(Options);
            foreach (string Executable in sources)
            {
                if (Canceled)
                {
                    break;
                }

                AllTestCasesInAllExecutables.AddRange(Discoverer.GetTestsFromExecutable(frameworkHandle, Executable));
            }
            RunTests(true, AllTestCasesInAllExecutables, AllTestCasesInAllExecutables, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);

            Canceled = false;
            List<TestCase> AllTestCasesInAllExecutables = new List<TestCase>();
            TestCase[] TestCasesToRun = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(Options);
            foreach (string Executable in testCasesToRun.Select(TC => TC.Source).Distinct())
            {
                AllTestCasesInAllExecutables.AddRange(discoverer.GetTestsFromExecutable(frameworkHandle, Executable));
            }
            RunTests(false, AllTestCasesInAllExecutables, TestCasesToRun, runContext, frameworkHandle);
        }

        private void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            IGoogleTestRunner runner;
            string testDirectory;
            if (Options.ParallelTestExecution)
            {
                runner = new ParallelTestRunner(Options);
                testDirectory = null;
            }
            else
            {
                runner = new SequentialTestRunner(Options);
                testDirectory = Utils.GetTempDirectory();
            }
            runner.RunTests(runAllTestCases, allTestCases, testCasesToRun, runContext, handle, testDirectory);
            handle.SendMessage(TestMessageLevel.Informational, "GTA: Test execution completed.");
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

    }

}