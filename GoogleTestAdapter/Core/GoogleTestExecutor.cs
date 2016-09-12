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

        private readonly TestEnvironment _testEnvironment;

        private ITestRunner _runner;
        private bool _canceled;

        public GoogleTestExecutor(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<TestCase> allTestCasesInExecutables, IEnumerable<TestCase> testCasesToRun, ITestFrameworkReporter reporter, IDebuggedProcessLauncher launcher, bool isBeingDebugged, string solutionDirectory, IProcessExecutor executor)
        {
            _testEnvironment.DebugInfo(_testEnvironment.Options.ToString());

            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            _testEnvironment.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (_canceled)
                {
                    return;
                }
                ComputeTestRunner(reporter, isBeingDebugged, solutionDirectory);
            }

            _runner.RunTests(allTestCasesInExecutables, testCasesToRunAsArray, solutionDirectory, null, null, isBeingDebugged, launcher, executor);
        }

        public void Cancel()
        {
            lock (this)
            {
                _canceled = true;
                _runner?.Cancel();
            }
        }

        private void ComputeTestRunner(ITestFrameworkReporter reporter, bool isBeingDebugged, string solutionDirectory)
        {
            if (_testEnvironment.Options.ParallelTestExecution && !isBeingDebugged)
            {
                _runner = new ParallelTestRunner(reporter, _testEnvironment, solutionDirectory);
            }
            else
            {
                _runner = new PreparingTestRunner(solutionDirectory, reporter, _testEnvironment);
                if (_testEnvironment.Options.ParallelTestExecution && isBeingDebugged)
                {
                    _testEnvironment.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

    }

}