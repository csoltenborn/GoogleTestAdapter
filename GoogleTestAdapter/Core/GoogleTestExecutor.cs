using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {

        private TestEnvironment TestEnvironment { get; }

        private ITestRunner Runner { get; set; }
        private bool Canceled { get; set; } = false;

        public GoogleTestExecutor(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<TestCase> allTestCasesInExecutables, IEnumerable<TestCase> testCasesToRun, ITestFrameworkReporter reporter, IDebuggedProcessLauncher launcher, bool isBeingDebugged, string solutionDirectory)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            TestEnvironment.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (Canceled)
                {
                    return;
                }
                ComputeTestRunner(reporter, isBeingDebugged, solutionDirectory);
            }

            Runner.RunTests(allTestCasesInExecutables, testCasesToRunAsArray, solutionDirectory, null, isBeingDebugged, launcher);
            TestEnvironment.LogInfo("Test execution completed.");
        }

        public void Cancel()
        {
            lock (this)
            {
                Canceled = true;
                Runner?.Cancel();
            }
        }

        private void ComputeTestRunner(ITestFrameworkReporter reporter, bool isBeingDebugged, string solutionDirectory)
        {
            if (TestEnvironment.Options.ParallelTestExecution && !isBeingDebugged)
            {
                Runner = new ParallelTestRunner(reporter, TestEnvironment, solutionDirectory);
            }
            else
            {
                Runner = new PreparingTestRunner(0, solutionDirectory, reporter, TestEnvironment);
                if (TestEnvironment.Options.ParallelTestExecution && isBeingDebugged)
                {
                    TestEnvironment.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

    }

}