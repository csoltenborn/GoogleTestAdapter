using System;
using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestCases;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly IDebuggedProcessExecutorFactory _processExecutorFactory;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private ITestRunner _runner;
        private bool _canceled;

        public GoogleTestExecutor(ILogger logger, SettingsWrapper settings, IDebuggedProcessExecutorFactory processExecutorFactory)
        {
            _logger = logger;
            _settings = settings;
            _processExecutorFactory = processExecutorFactory;
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

            if (!string.IsNullOrWhiteSpace(_settings.ReturnCodeTestCase))
            {
                bool printWarning = false;
                foreach (ExecutableResult executableResult in _runner.ExecutableResults)
                {
                    var resultCodeTestCase = TestCaseFactory.CreateResultCodeTestCase(_settings.ReturnCodeTestCase, executableResult.Executable);
                    reporter.ReportTestsStarted(resultCodeTestCase.Yield());

                    int resultCode = executableResult.ResultCode;
                    if (resultCode == 0)
                    {
                        var testResult =
                            StreamingStandardOutputTestResultParser.CreatePassedTestResult(resultCodeTestCase,
                                TimeSpan.Zero);
                        if (executableResult.ResultCodeOutput.Any())
                        {
                            string message = $"{Environment.NewLine}{Environment.NewLine}Output:{Environment.NewLine}";
                            message += string.Join(Environment.NewLine, executableResult.ResultCodeOutput);
                            testResult.ErrorMessage = message;
                        }

                        reporter.ReportTestResults(testResult.Yield());
                    }
                    else
                    {
                        string message = $"Exit code: {resultCode}";
                        if (executableResult.ResultCodeOutput.Any())
                        {
                            message += $"{Environment.NewLine}{Environment.NewLine}Output:{Environment.NewLine}";
                            message += string.Join(Environment.NewLine, executableResult.ResultCodeOutput);
                        } 
                        else if (isBeingDebugged && !_settings.UseNewTestExecutionFramework)
                        {
                            printWarning = true;
                        }

                        reporter.ReportTestResults(StreamingStandardOutputTestResultParser.CreateFailedTestResult(resultCodeTestCase, TimeSpan.Zero, message, "").Yield());
                    }
                }

                if (printWarning)
                {
                    _logger.LogWarning($"Result code output can not be collected while debugging if option '{SettingsWrapper.OptionUseNewTestExecutionFramework}' is false");
                }
            }

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