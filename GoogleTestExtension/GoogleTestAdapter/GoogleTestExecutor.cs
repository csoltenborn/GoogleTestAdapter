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

        private IGoogleTestRunner Runner { get; set; }

        public GoogleTestExecutor() : this(null) { }

        internal GoogleTestExecutor(AbstractOptions options) : base(options) {}

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);

                List<TestCase> allTestCasesInAllExecutables = new List<TestCase>();
                GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(Options);
                foreach (string executable in sources)
                {
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

        public void Cancel()
        {
            DebugUtils.CheckDebugModeForExecutionCode();

            Runner.Cancel();
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

            handle.SendMessage(TestMessageLevel.Informational, "GTA: Running " + testCasesToRun.Count() + " tests...");
            runner.RunTests(runAllTestCases, allTestCases, testCasesToRun, runContext, handle, userParameters);
            handle.SendMessage(TestMessageLevel.Informational, "GTA: Test execution completed.");
        }

    }

}