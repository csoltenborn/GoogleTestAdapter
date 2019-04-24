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
using GoogleTestAdapter.TestCases;

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

        public static TestCase CreateExitCodeTestCase(SettingsWrapper settings, string executable, TestCaseLocation mainMethodLocation = null)
        {
            string filename = Path.GetFileName(executable) ?? throw new InvalidOperationException($"Can't get filename from executable '{executable}'");
            string testCaseName = $"{settings.ExitCodeTestCase}.{filename.Replace(".", "_")}";
            string sourceFile = mainMethodLocation?.Sourcefile ?? "";
            int line = (int) (mainMethodLocation?.Line ?? 0);

            return new TestCase(testCaseName, executable, testCaseName, sourceFile, line);
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
                        var testResult = ReportExitCodeTestResult(executableResult);
                        printWarning |= isBeingDebugged && !_settings.UseNewTestExecutionFramework;
                        _logger.DebugInfo($"Reported exit code test {testResult.DisplayName} for executable {executableResult.Executable}");
                    }
                });
            }

            if (printWarning)
            {
                _logger.LogWarning(
                    $"Result code output can not be collected while debugging if option '{SettingsWrapper.OptionUseNewTestExecutionFramework}' is false");
            }
        }

        private TestResult ReportExitCodeTestResult(ExecutableResult executableResult)
        {
            var exitCodeTestCase = CreateExitCodeTestCase(_settings, executableResult.Executable);
            _reporter.ReportTestsStarted(exitCodeTestCase.Yield());

            var testResult = CreateExitCodeTestResult(exitCodeTestCase, executableResult);

            _reporter.ReportTestResults(testResult.Yield());
            return testResult;
        }

        private TestResult CreateExitCodeTestResult(TestCase exitCodeTestCase, ExecutableResult executableResult)
        {
            string message = "";
            
            if (executableResult.ExitCode != 0)
            {
                message += $"Exit code: {executableResult.ExitCode}";
            }

            if (executableResult.ExitCodeOutput.Any())
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}";
                }
                message += string.Join(Environment.NewLine, executableResult.ExitCodeOutput);
            }

            var result = StreamingStandardOutputTestResultParser
                .CreateFailedTestResult(exitCodeTestCase, TimeSpan.Zero, message, "");

            result.Outcome = executableResult.ExitCodeSkip
                ? TestOutcome.Skipped
                : executableResult.ExitCode == 0
                    ? TestOutcome.Passed
                    : TestOutcome.Failed;

            return result;
        }
    }
}