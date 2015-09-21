using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class GoogleTestExecutor : AbstractOptionsProvider, ITestExecutor
    {
        internal const string ExecutorUriString = Constants.IdentifierUri;
        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private bool Canceled { get; set; } = false;
        private IGoogleTestRunner Runner { get; set; }
        private List<TestCase> AllTestCasesInAllExecutables { get; } = new List<TestCase>();

        public GoogleTestExecutor() : this(null) { }

        internal GoogleTestExecutor(AbstractOptions options) : base(options) { }

        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                lock (this)
                {
                    DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);
                    ComputeTestRunner(runContext, frameworkHandle);
                }

                ComputeAllTestCasesInAllExecutables(executables, frameworkHandle);
                RunTests(true, AllTestCasesInAllExecutables, runContext, frameworkHandle);
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
                lock (this)
                {
                    DebugUtils.CheckDebugModeForExecutionCode(frameworkHandle);
                    ComputeTestRunner(runContext, frameworkHandle);
                }

                TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
                ComputeAllTestCasesInAllExecutables(testCasesToRunAsArray.Select(tc => tc.Source).Distinct(), frameworkHandle);
                RunTests(false, testCasesToRunAsArray, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, "GTA: Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            lock (this)
            {
                DebugUtils.CheckDebugModeForExecutionCode();

                Canceled = true;
                Runner.Cancel();
            }
        }

        private void RunTests(bool runAllTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            handle.SendMessage(TestMessageLevel.Informational, "GTA: Running " + testCasesToRunAsArray.Length + " tests...");
            Runner.RunTests(runAllTestCases, AllTestCasesInAllExecutables, testCasesToRunAsArray, null, runContext, handle);
            handle.SendMessage(TestMessageLevel.Informational, "GTA: Test execution completed.");
        }

        private void ComputeTestRunner(IRunContext runContext, IMessageLogger logger)
        {
            if (Options.ParallelTestExecution && !runContext.IsBeingDebugged)
            {
                Runner = new ParallelTestRunner(Options);
            }
            else
            {
                Runner = new PreparingTestRunner(0, Options);
                if (Options.ParallelTestExecution && runContext.IsBeingDebugged)
                {
                    DebugUtils.LogUserDebugMessage(logger, Options,
                        TestMessageLevel.Informational,
                        "GTA: Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

        private void ComputeAllTestCasesInAllExecutables(IEnumerable<string> executables, IMessageLogger logger)
        {
            AllTestCasesInAllExecutables.Clear();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(Options);
            foreach (string executable in executables)
            {
                if (Canceled)
                {
                    AllTestCasesInAllExecutables.Clear();
                    break;
                }

                AllTestCasesInAllExecutables.AddRange(discoverer.GetTestsFromExecutable(executable, logger));
            }
        }

    }

}