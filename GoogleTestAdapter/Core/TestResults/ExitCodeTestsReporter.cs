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
    public class ExitCodeTestsReporter : IExitCodeTestsReporter
    {
        private readonly ITestFrameworkReporter _reporter;
        private readonly IExitCodeTestsAggregator _exitCodeTestsAggregator;
        private readonly SettingsWrapper _settings;
        private readonly ILogger _logger;

        public ExitCodeTestsReporter(ITestFrameworkReporter reporter, IExitCodeTestsAggregator exitCodeTestsAggregator, 
            SettingsWrapper settings, ILogger logger)
        {
            _reporter = reporter;
            _exitCodeTestsAggregator = exitCodeTestsAggregator;
            _settings = settings;
            _logger = logger;
        }

        public static TestCase CreateExitCodeTestCase(SettingsWrapper settings, string executable)
        {
            string filename = Path.GetFileName(executable) ?? throw new InvalidOperationException($"Can't get filename from executable '{executable}'");
            string testCaseName = $"{filename.Replace(".", "_")}.{settings.ExitCodeTestCase}";
            return new TestCase(testCaseName, executable, testCaseName, "", 0);
        }

        public void ReportExitCodeTestCases(IEnumerable<ExecutableResult> allResults, bool isBeingDebugged)
        {
            var aggregatedResults = _exitCodeTestsAggregator.ComputeAggregatedResults(allResults);

            bool printWarning = false;
            foreach (var executableResult in aggregatedResults)
            {
                _settings.ExecuteWithSettingsForExecutable(executableResult.Executable, _logger, () =>
                {
                    if (!string.IsNullOrWhiteSpace(_settings.ExitCodeTestCase))
                    {
                        ReportExitCodeTestResult(executableResult);
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

        private void ReportExitCodeTestResult(ExecutableResult executableResult)
        {
            var exitCodeTestCase = CreateExitCodeTestCase(_settings, executableResult.Executable);
            _reporter.ReportTestsStarted(exitCodeTestCase.Yield());

            TestResult testResult;
            if (executableResult.ExitCode == 0)
            {
                testResult = CreatePassingExitCodeTestResult(exitCodeTestCase, executableResult);
                if (executableResult.ExitCodeSkip)
                {
                    testResult.Outcome = TestOutcome.Skipped;
                }
            }
            else
            {
                testResult = CreateFailingExitCodeTestResult(exitCodeTestCase, executableResult, executableResult.ExitCode);
            }

            _reporter.ReportTestResults(testResult.Yield());
        }

        private TestResult CreatePassingExitCodeTestResult(TestCase exitCodeTestCase,
            ExecutableResult executableResult)
        {
            var testResult =
                StreamingStandardOutputTestResultParser.CreatePassedTestResult(exitCodeTestCase,
                    TimeSpan.Zero);
            if (executableResult.ExitCodeOutput.Any())
            {
                testResult.ErrorMessage = string.Join(Environment.NewLine, executableResult.ExitCodeOutput);
            }

            return testResult;
        }

        private TestResult CreateFailingExitCodeTestResult(TestCase exitCodeTestCase, ExecutableResult executableResult,
            int exitCode)
        {
            string message = $"Exit code: {exitCode}";
            if (executableResult.ExitCodeOutput.Any())
            {
                message += $"{Environment.NewLine}{Environment.NewLine}";
                message += string.Join(Environment.NewLine, executableResult.ExitCodeOutput);
            }

            return StreamingStandardOutputTestResultParser
                .CreateFailedTestResult(exitCodeTestCase, TimeSpan.Zero, message, "");
        }
    }
}