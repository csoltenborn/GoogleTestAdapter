using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Execution;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class GoogleTestExecutor : AbstractGoogleTestAdapterClass, ITestExecutor
    {
        internal const string ExecutorUriString = Constants.IdentifierUri;
        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private bool Canceled { get; set; } = false;

        public GoogleTestExecutor() : this(null) { }

        internal GoogleTestExecutor(AbstractOptions options) : base(options) {}

        public void Cancel()
        {
            DebugUtils.CheckDebugModeForExecutionCode();

            Canceled = true;
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);

                Canceled = false;

                List<TestCase> allTestCasesInAllExecutables = new List<TestCase>();
                GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(Options);
                foreach (string executable in sources)
                {
                    if (Canceled)
                    {
                        break;
                    }

                    allTestCasesInAllExecutables.AddRange(discoverer.GetTestsFromExecutable(frameworkHandle, executable));
                }
                RunTests(true, allTestCasesInAllExecutables, allTestCasesInAllExecutables, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, "GTA: Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);

                Canceled = false;
                List<TestCase> allTestCasesInAllExecutables = new List<TestCase>();
                TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

                GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(Options);
                foreach (string executable in testCasesToRunAsArray.Select(tc => tc.Source).Distinct())
                {
                    allTestCasesInAllExecutables.AddRange(discoverer.GetTestsFromExecutable(frameworkHandle, executable));
                }
                RunTests(false, allTestCasesInAllExecutables, testCasesToRunAsArray, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, "GTA: Exception while running tests: " + e);
            }
        }

        private void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            IGoogleTestRunner runner;
            string userParameters;
            if (Options.ParallelTestExecution)
            {
                runner = new ParallelTestRunner(Options);
                userParameters = null;
            }
            else
            {
                runner = new SequentialTestRunner(Options);
                userParameters = Options.GetUserParameters(runContext.SolutionDirectory, Utils.GetTempDirectory(), 0);
            }
            runner.RunTests(runAllTestCases, allTestCases, testCasesToRun, runContext, handle, userParameters);
            handle.SendMessage(TestMessageLevel.Informational, "GTA: Test execution completed.");
        }

        internal static IDictionary<string, List<TestCase>> GroupTestcasesByExecutable(IEnumerable<TestCase> testcases)
        {
            Dictionary<string, List<TestCase>> groupedTestCases = new Dictionary<string, List<TestCase>>();
            foreach (TestCase testCase in testcases)
            {
                List<TestCase> group;
                if (groupedTestCases.ContainsKey(testCase.Source))
                {
                    group = groupedTestCases[testCase.Source];
                }
                else
                {
                    group = new List<TestCase>();
                    groupedTestCases.Add(testCase.Source, group);
                }
                group.Add(testCase);
            }
            return groupedTestCases;
        }

    }

}