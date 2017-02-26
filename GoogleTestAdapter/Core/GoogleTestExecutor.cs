using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly IDebuggerAttacher _debuggerAttacher;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private ITestRunner _runner;
        private bool _canceled;

        public GoogleTestExecutor(ILogger logger, SettingsWrapper settings, IDebuggerAttacher debuggerAttacher)
        {
            _logger = logger;
            _settings = settings;
            _debuggerAttacher = debuggerAttacher;
            _schedulingAnalyzer = new SchedulingAnalyzer(logger);
        }


        public void RunTests(IEnumerable<TestCase> allTestCasesInExecutables, IEnumerable<TestCase> testCasesToRun, ITestFrameworkReporter reporter, bool isBeingDebugged, string solutionDirectory)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            _logger.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (_canceled)
                {
                    return;
                }
                ComputeTestRunner(reporter, isBeingDebugged, solutionDirectory);
            }

            _runner.RunTests(allTestCasesInExecutables, testCasesToRunAsArray, solutionDirectory, null, null);

            if (_settings.ParallelTestExecution)
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
            if (_settings.ParallelTestExecution && !isBeingDebugged)
            {
                _runner = new ParallelTestRunner(reporter, _logger, _settings, solutionDirectory, _schedulingAnalyzer, _debuggerAttacher);
            }
            else
            {
                _runner = new PreparingTestRunner(solutionDirectory, reporter, _logger, _settings, _schedulingAnalyzer, _debuggerAttacher);
                if (_settings.ParallelTestExecution && isBeingDebugged)
                {
                    _logger.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

    }

}