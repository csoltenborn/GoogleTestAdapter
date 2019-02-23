using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestResults
{
    public class ResultCodeTestsReporter : IResultCodeTestsReporter
    {
        private readonly ITestFrameworkReporter _reporter;
        private readonly IResultCodeTestsAggregator _resultCodeTestsAggregator;
        private readonly SettingsWrapper _settings;
        private readonly ILogger _logger;

        public ResultCodeTestsReporter(ITestFrameworkReporter reporter, IResultCodeTestsAggregator resultCodeTestsAggregator, 
            SettingsWrapper settings, ILogger logger)
        {
            _reporter = reporter;
            _resultCodeTestsAggregator = resultCodeTestsAggregator;
            _settings = settings;
            _logger = logger;
        }

        public static TestCase CreateResultCodeTestCase(SettingsWrapper settings, string executable)
        {
            string filename = Path.GetFileName(executable) ?? throw new InvalidOperationException($"Can't get filename from executable '{executable}'");
            string testCaseName = $"{filename.Replace(".", "_")}.{settings.ReturnCodeTestCase}";
            return new TestCase(testCaseName, executable, testCaseName, "", 0);
        }

        public void ReportResultCodeTestCases(IEnumerable<ExecutableResult> allResults, bool isBeingDebugged)
        {
            var aggregatedResults = _resultCodeTestsAggregator.ComputeAggregatedResults(allResults);

            bool printWarning = false;
            foreach (var executableResult in aggregatedResults)
            {
                _settings.ExecuteWithSettingsForExecutable(executableResult.Executable, _logger, () =>
                {
                    if (!string.IsNullOrWhiteSpace(_settings.ReturnCodeTestCase))
                    {
                        ReportResultCodeTestResult(executableResult);
                        printWarning |= isBeingDebugged && !_settings.UseNewTestExecutionFramework;
                    }
                });
            }

            if (printWarning)
            {
                _logger.LogWarning(
                    $"Result code output can not be collected while debugging if option '{SettingsWrapper.OptionUseNewTestExecutionFramework}' is false");
            }
        }

        private void ReportResultCodeTestResult(ExecutableResult executableResult)
        {
            var resultCodeTestCase = CreateResultCodeTestCase(_settings, executableResult.Executable);
            _reporter.ReportTestsStarted(resultCodeTestCase.Yield());

            TestResult testResult;
            if (executableResult.ResultCode == 0)
            {
                testResult = CreatePassingResultCodeTestResult(resultCodeTestCase, executableResult);
                if (executableResult.ResultCodeSkip)
                {
                    testResult.Outcome = TestOutcome.Skipped;
                }
            }
            else
            {
                testResult = CreateFailingResultCodeTestResult(resultCodeTestCase, executableResult, executableResult.ResultCode);
            }

            _reporter.ReportTestResults(testResult.Yield());
        }

        private TestResult CreatePassingResultCodeTestResult(TestCase resultCodeTestCase,
            ExecutableResult executableResult)
        {
            var testResult =
                StreamingStandardOutputTestResultParser.CreatePassedTestResult(resultCodeTestCase,
                    TimeSpan.Zero);
            if (executableResult.ResultCodeOutput.Any())
            {
                testResult.ErrorMessage = string.Join(Environment.NewLine, executableResult.ResultCodeOutput);
            }

            return testResult;
        }

        private TestResult CreateFailingResultCodeTestResult(TestCase resultCodeTestCase, ExecutableResult executableResult,
            int resultCode)
        {
            string message = $"Exit code: {resultCode}";
            if (executableResult.ResultCodeOutput.Any())
            {
                message += $"{Environment.NewLine}{Environment.NewLine}";
                message += string.Join(Environment.NewLine, executableResult.ResultCodeOutput);
            }

            return StreamingStandardOutputTestResultParser
                .CreateFailedTestResult(resultCodeTestCase, TimeSpan.Zero, message, "");
        }
    }
}