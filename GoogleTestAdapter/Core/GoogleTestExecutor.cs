using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
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

            HandleResultCodeTestCases(reporter, isBeingDebugged);

            if (_settings.ParallelTestExecution)
                _schedulingAnalyzer.PrintStatisticsToDebugOutput();
        }

        private void HandleResultCodeTestCases(ITestFrameworkReporter reporter, bool isBeingDebugged)
        {
            Debug.Assert(_runner.ExecutableResults.Select(r => r.Executable).Distinct().Count() == _runner.ExecutableResults.Count);

            bool printWarning = false;
            foreach (ExecutableResult executableResult in _runner.ExecutableResults)
            {
                _settings.ExecuteWithSettingsForExecutable(executableResult.Executable, () =>
                {
                    if (!string.IsNullOrWhiteSpace(_settings.ReturnCodeTestCase))
                    {
                        var resultCodeTestCase =
                            TestCaseFactory.CreateResultCodeTestCase(_settings, executableResult.Executable);
                        reporter.ReportTestsStarted(resultCodeTestCase.Yield());

                        int resultCode = executableResult.ResultCode;
                        if (resultCode == 0)
                        {
                            var testResult = CreatePassingResultCodeTestResult(resultCodeTestCase, executableResult);
                            reporter.ReportTestResults(testResult.Yield());
                        }
                        else
                        {
                            var testResult = CreateFailingResultCodeTestResult(resultCodeTestCase, executableResult, resultCode, isBeingDebugged, ref printWarning);
                            reporter.ReportTestResults(testResult.Yield());
                        }
                    }
                }, _logger);
            }

            if (printWarning)
            {
                _logger.LogWarning(
                    $"Result code output can not be collected while debugging if option '{SettingsWrapper.OptionUseNewTestExecutionFramework}' is false");
            }
        }

        private TestResult CreateFailingResultCodeTestResult(TestCase resultCodeTestCase, ExecutableResult executableResult,
            int resultCode, bool isBeingDebugged, ref bool printWarning)
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

            return StreamingStandardOutputTestResultParser
                .CreateFailedTestResult(resultCodeTestCase, TimeSpan.Zero, message, "");
        }

        private static TestResult CreatePassingResultCodeTestResult(TestCase resultCodeTestCase,
            ExecutableResult executableResult)
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

            return testResult;
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