// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter.Runners
{
    public class TestResultCollector
    {
        private readonly ILogger _logger;
        private readonly string _threadName;

        public TestResultCollector(ILogger logger, string threadName)
        {
            _logger = logger;
            _threadName = threadName;
        }

        public List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, List<string> consoleOutput, TestCase crashedTestCase)
        {
            var testResults = new List<TestResult>();
            TestCase[] arrTestCasesRun = testCasesRun as TestCase[] ?? testCasesRun.ToArray();

            var consoleParser = new StandardOutputTestResultParser(arrTestCasesRun, consoleOutput, _logger);
            if (testResults.Count < arrTestCasesRun.Length)
                CollectResultsFromConsoleOutput(consoleParser, testResults);

            if (testResults.Count < arrTestCasesRun.Length)
            {
                if (crashedTestCase == null)
                    crashedTestCase = consoleParser.CrashedTestCase;

                var remainingTestCases = arrTestCasesRun
                    .Where(tc => !testResults.Exists(tr => tr.TestCase.FullyQualifiedName == tc.FullyQualifiedName))
                    .ToArray();

                if (crashedTestCase != null)
                    CreateMissingResults(remainingTestCases, crashedTestCase, testResults);
                else
                    ReportSuspiciousTestCases(remainingTestCases);
            }

            return testResults;
        }

        private void CollectResultsFromConsoleOutput(StandardOutputTestResultParser consoleParser, List<TestResult> testResults)
        {
            List<TestResult> consoleResults = consoleParser.GetTestResults();
            int nrOfCollectedTestResults = 0;
            foreach (TestResult testResult in consoleResults.Where(
                tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
            {
                testResults.Add(testResult);
                nrOfCollectedTestResults++;
            }
            if (nrOfCollectedTestResults > 0)
                _logger.DebugInfo(String.Format(Resources.CollectedResultsFromConsole, _threadName, nrOfCollectedTestResults));
        }

        private void CreateMissingResults(TestCase[] testCases, TestCase crashedTestCase, List<TestResult> testResults)
        {
            var errorMessage = String.Format(Resources.CrashTest, crashedTestCase.DisplayName);
            var errorStackTrace = ErrorMessageParser.CreateStackTraceEntry("crash suspect",
                crashedTestCase.CodeFilePath, crashedTestCase.LineNumber.ToString());

            foreach (TestCase testCase in testCases)
            {
                testResults.Add(new TestResult(testCase)
                {
                    ComputerName = Environment.MachineName,
                    Outcome = TestOutcome.Skipped,
                    ErrorMessage = errorMessage,
                    ErrorStackTrace = errorStackTrace
                });
            }
            if (testCases.Length > 0)
                _logger.DebugInfo(String.Format(Resources.CreatedTestResults, _threadName, testCases.Length));
        }

        private void ReportSuspiciousTestCases(TestCase[] testCases)
        {
            string testCasesAsString = string.Join(Environment.NewLine, testCases.Select(tc => tc.DisplayName));
            _logger.DebugWarning(String.Format(Resources.TestCaseNotRun, _threadName, testCases.Length, Environment.NewLine, testCasesAsString));
        }

    }
}