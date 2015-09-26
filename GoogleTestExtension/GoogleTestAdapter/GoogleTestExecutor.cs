using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class GoogleTestExecutor : ITestExecutor
    {
        internal const string ExecutorUriString = "executor://GoogleTestRunner/v1";
        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private TestEnvironment TestEnvironment { get; set; }

        private bool Canceled { get; set; } = false;
        private ITestRunner Runner { get; set; }
        private List<TestCase> AllTestCasesInAllExecutables { get; } = new List<TestCase>();

        public GoogleTestExecutor() : this(null) { }

        internal GoogleTestExecutor(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }

        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                if (TestEnvironment == null)
                {
                    TestEnvironment = new TestEnvironment(new Options(), frameworkHandle);
                }

                TestEnvironment.CheckDebugModeForExecutionCode();

                ComputeTestRunner(runContext);
                ComputeAllTestCasesInAllExecutables(executables);

                RunTests(true, AllTestCasesInAllExecutables, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("GTA: Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                if (TestEnvironment == null)
                {
                    TestEnvironment = new TestEnvironment(new Options(), frameworkHandle);
                }

                TestEnvironment.CheckDebugModeForExecutionCode();

                TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
                ComputeTestRunner(runContext);
                ComputeAllTestCasesInAllExecutables(testCasesToRunAsArray.Select(tc => tc.Source).Distinct());

                RunTests(false, testCasesToRunAsArray, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("GTA: Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            Canceled = true;
            Runner.Cancel();
        }

        private void RunTests(bool runAllTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            TestEnvironment.LogInfo("GTA: Running " + testCasesToRunAsArray.Length + " tests...");

            Runner.RunTests(runAllTestCases, AllTestCasesInAllExecutables, testCasesToRunAsArray, null, runContext, handle);
            TestEnvironment.LogInfo("GTA: Test execution completed.");
        }

        private void ComputeTestRunner(IRunContext runContext)
        {
            if (TestEnvironment.Options.ParallelTestExecution && !runContext.IsBeingDebugged)
            {
                Runner = new ParallelTestRunner(TestEnvironment);
            }
            else
            {
                Runner = new PreparingTestRunner(0, TestEnvironment);
                if (TestEnvironment.Options.ParallelTestExecution && runContext.IsBeingDebugged)
                {
                    TestEnvironment.LogInfo(
                        "GTA: Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.",
                        TestEnvironment.LogType.UserDebug);
                }
            }
        }

        private void ComputeAllTestCasesInAllExecutables(IEnumerable<string> executables)
        {
            AllTestCasesInAllExecutables.Clear();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            foreach (string executable in executables)
            {
                if (Canceled)
                {
                    AllTestCasesInAllExecutables.Clear();
                    break;
                }

                AllTestCasesInAllExecutables.AddRange(discoverer.GetTestsFromExecutable(executable));
            }
        }

    }

}