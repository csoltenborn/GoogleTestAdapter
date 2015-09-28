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

        private List<TestCase> AllTestCasesInExecutables { get; } = new List<TestCase>();
        private ITestRunner Runner { get; set; }
        private bool Canceled { get; set; } = false;


        public GoogleTestExecutor() : this(null) { }

        internal GoogleTestExecutor(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                InitTestEnvironment(frameworkHandle);

                ComputeAllTestCasesInExecutables(executables);

                DoRunTests(AllTestCasesInExecutables, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                InitTestEnvironment(frameworkHandle);

                TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
                ComputeAllTestCasesInExecutables(testCasesToRunAsArray.Select(tc => tc.Source).Distinct());

                DoRunTests(testCasesToRunAsArray, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            lock (this)
            {
                Canceled = true;
                Runner?.Cancel();
                TestEnvironment.LogInfo("Test execution canceled.");
            }
        }

        private void InitTestEnvironment(IFrameworkHandle frameworkHandle)
        {
            if (TestEnvironment == null)
            {
                TestEnvironment = new TestEnvironment(new Options(frameworkHandle), frameworkHandle);
            }

            TestEnvironment.CheckDebugModeForExecutionCode();
        }

        private void DoRunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            TestEnvironment.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (Canceled)
                {
                    return;
                }
                ComputeTestRunner(runContext);
            }

            Runner.RunTests(AllTestCasesInExecutables, testCasesToRunAsArray, null, runContext, handle);
            TestEnvironment.LogInfo("Test execution completed.");
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
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.",
                        TestEnvironment.LogType.UserDebug);
                }
            }
        }

        private void ComputeAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            AllTestCasesInExecutables.Clear();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            foreach (string executable in executables)
            {
                if (Canceled)
                {
                    AllTestCasesInExecutables.Clear();
                    break;
                }

                AllTestCasesInExecutables.AddRange(discoverer.GetTestsFromExecutable(executable));
            }
        }

    }

}