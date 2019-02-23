using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly IDebuggedProcessExecutorFactory _processExecutorFactory;
        private readonly IExitCodeTestsReporter _exitCodeTestsReporter;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private ITestRunner _runner;
        private bool _canceled;

        public GoogleTestExecutor(ILogger logger, SettingsWrapper settings, IDebuggedProcessExecutorFactory processExecutorFactory, IExitCodeTestsReporter exitCodeTestsReporter)
        {
            _logger = logger;
            _settings = settings;
            _processExecutorFactory = processExecutorFactory;
            _exitCodeTestsReporter = exitCodeTestsReporter;
            _schedulingAnalyzer = new SchedulingAnalyzer(logger);
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, ITestFrameworkReporter reporter, bool isBeingDebugged)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            _logger.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (_canceled)
                {
                    return;
                }
                ComputeTestRunner(reporter, isBeingDebugged);
            }

            _runner.RunTests(testCasesToRunAsArray, isBeingDebugged, _processExecutorFactory);

            _exitCodeTestsReporter.ReportExitCodeTestCases(_runner.ExecutableResults, isBeingDebugged);

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

        private void ComputeTestRunner(ITestFrameworkReporter reporter, bool isBeingDebugged)
        {
            if (_settings.ParallelTestExecution && !isBeingDebugged)
            {
                _runner = new ParallelTestRunner(reporter, _logger, _settings, _schedulingAnalyzer);
            }
            else
            {
                _runner = new PreparingTestRunner(reporter, _logger, _settings, _schedulingAnalyzer);
                if (_settings.ParallelTestExecution && isBeingDebugged)
                {
                    _logger.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

    }

}