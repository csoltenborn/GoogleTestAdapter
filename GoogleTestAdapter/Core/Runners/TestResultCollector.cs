using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter.Runners
{
    public class TestResultCollector
    {
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;

        private readonly string _threadName;

        public TestResultCollector(ILogger logger, string threadName, SettingsWrapper settings)
        {
            _logger = logger;
            _threadName = threadName;
            _settings = settings;
        }

        public List<TestResult> CollectTestResults(IEnumerable<TestCase> testCasesRun, string testExecutable, string resultXmlFile, List<string> consoleOutput, TestCase crashedTestCase)
        {
            var testResults = new List<TestResult>();
            TestCase[] arrTestCasesRun = testCasesRun as TestCase[] ?? testCasesRun.ToArray();

            if (testResults.Count < arrTestCasesRun.Length)
                CollectResultsFromXmlFile(arrTestCasesRun, testExecutable, resultXmlFile, testResults);

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
                    ReportSuspiciousTestCases(remainingTestCases, testResults);
            }

            return testResults;
        }

        private void CollectResultsFromXmlFile(TestCase[] testCasesRun, string testExecutable, string resultXmlFile, List<TestResult> testResults)
        {
            var xmlParser = new XmlTestResultParser(testCasesRun, testExecutable, resultXmlFile, _logger);
            List<TestResult> xmlResults = xmlParser.GetTestResults();
            int nrOfCollectedTestResults = 0;
            foreach (TestResult testResult in xmlResults.Where(
                tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
            {
                testResults.Add(testResult);
                nrOfCollectedTestResults++;
            }
            if (nrOfCollectedTestResults > 0)
                _logger.DebugInfo(
                    $"{_threadName}Collected {nrOfCollectedTestResults} test results from result XML file {resultXmlFile}");
        }

        private void CollectResultsFromConsoleOutput(StandardOutputTestResultParser consoleParser, List<TestResult> testResults)
        {
            var consoleResults = consoleParser.GetTestResults();
            int nrOfCollectedTestResults = 0;
            foreach (TestResult testResult in consoleResults.Where(
                tr => !testResults.Exists(tr2 => tr.TestCase.FullyQualifiedName == tr2.TestCase.FullyQualifiedName)))
            {
                testResults.Add(testResult);
                nrOfCollectedTestResults++;
            }
            if (nrOfCollectedTestResults > 0)
                _logger.DebugInfo($"{_threadName}Collected {nrOfCollectedTestResults} test results from console output");
        }

        private void CreateMissingResults(TestCase[] testCases, TestCase crashedTestCase, List<TestResult> testResults)
        {
            var errorMessage = $"reason is probably a crash of test {crashedTestCase.DisplayName}";
            var errorStackTrace = ErrorMessageParser.CreateStackTraceEntry("crash suspect",
                crashedTestCase.CodeFilePath, crashedTestCase.LineNumber.ToString());

            testResults.AddRange(testCases.Select(testCase => 
                CreateTestResult(testCase, TestOutcome.Skipped, errorMessage, errorStackTrace)));
            if (testCases.Length > 0)
                _logger.DebugInfo($"{_threadName}Created {testCases.Length} test results for tests which were neither found in result XML file nor in console output");
        }

        private void ReportSuspiciousTestCases(TestCase[] testCases, List<TestResult> testResults)
        {
            string testCasesAsString = string.Join(Environment.NewLine, testCases.Select(tc => tc.DisplayName));
            _logger.DebugWarning(
                $"{_threadName}{testCases.Length} test cases seem to not have been run - are you repeating a test run, but tests have changed in the meantime? Test cases:{Environment.NewLine}{testCasesAsString}");

            string errorMessage = "Test case has not been run due to unkown reasons";
            switch (_settings.MissingTestsReportMode)
            {
                case MissingTestsReportMode.DoNotReport:
                    // nothing to do
                    break;
                case MissingTestsReportMode.ReportAsFailed:
                    testResults.AddRange(testCases.Select(tc => CreateTestResult(tc, TestOutcome.Failed, errorMessage, null)));
                    break;
                case MissingTestsReportMode.ReportAsSkipped:
                    testResults.AddRange(testCases.Select(tc => CreateTestResult(tc, TestOutcome.Skipped, errorMessage, null)));
                    break;
            }
        }

        private TestResult CreateTestResult(TestCase testCase, TestOutcome outcome, string errorMessage, string errorStackTrace)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                Outcome = outcome,
                ErrorMessage = errorMessage,
                ErrorStackTrace = errorStackTrace

            };
        }

    }
}