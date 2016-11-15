using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Scheduling;

namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {

        private readonly TestEnvironment _testEnvironment;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private ITestRunner _runner;
        private bool _canceled;

        public GoogleTestExecutor(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
            _schedulingAnalyzer = new SchedulingAnalyzer(testEnvironment);
        }


        public void RunTests(IEnumerable<TestCase> allTestCasesInExecutables, IEnumerable<TestCase> testCasesToRun, ITestFrameworkReporter reporter, IDebuggedProcessLauncher launcher, bool isBeingDebugged, string solutionDirectory, IProcessExecutor executor)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            _testEnvironment.Logger.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (_canceled)
                {
                    return;
                }
                ComputeTestRunner(reporter, isBeingDebugged, solutionDirectory);
            }

            _runner.RunTests(allTestCasesInExecutables, testCasesToRunAsArray, solutionDirectory, null, null, isBeingDebugged, launcher, executor);

            if (_testEnvironment.Options.ParallelTestExecution)
                _schedulingAnalyzer.PrintStatisticsToDebugOutput();
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
                _runner = new ParallelTestRunner(reporter, _testEnvironment, solutionDirectory, _schedulingAnalyzer);
            }
            else
            {
                _runner = new PreparingTestRunner(solutionDirectory, reporter, _testEnvironment, _schedulingAnalyzer);
                if (_testEnvironment.Options.ParallelTestExecution && isBeingDebugged)
                {
                    _testEnvironment.Logger.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

    }

}