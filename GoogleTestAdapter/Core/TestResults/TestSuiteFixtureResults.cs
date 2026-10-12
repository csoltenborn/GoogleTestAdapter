using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    /// <summary>
    /// Results reflecting failures of SetUpTestSuite() and TearDownTestSuite() of test suites: Google Test skips all
    /// tests of a suite whose SetUpTestSuite() fails, which are then reported as failed; failures of
    /// TearDownTestSuite() are reported by the suite's TearDownTestSuite test case (see option
    /// TearDownTestSuiteTestCases).
    /// </summary>
    public static class TestSuiteFixtureResults
    {
        public const string SetUpTestSuiteName = "SetUpTestSuite";

        public static string GetSuite(TestCase testCase)
        {
            // suite names may contain dots (e.g. "Namespace.Suite"), test names may not
            string fullyQualifiedName = testCase.FullyQualifiedName;
            return fullyQualifiedName.Substring(0, fullyQualifiedName.LastIndexOf('.'));
        }

        public static string GetSetUpTestSuiteName(string suite)
        {
            return $"{suite}.{SetUpTestSuiteName}";
        }

        public static string GetTearDownTestSuiteName(string suite)
        {
            return $"{suite}.{GoogleTestConstants.TearDownTestSuiteTestName}";
        }

        public static TestCase FindTearDownTestSuiteTestCase(string suite, IEnumerable<TestCase> testCases)
        {
            string name = GetTearDownTestSuiteName(suite);
            return testCases.FirstOrDefault(tc => tc.IsTearDownTestSuiteTestCase && tc.FullyQualifiedName == name);
        }

        /// <summary>
        /// The result of a test skipped by Google Test since SetUpTestSuite() of its suite has failed
        /// </summary>
        public static TestResult CreateSetUpTestSuiteFailedResult(TestResult skippedResult, string errorMessage, string errorStackTrace)
        {
            return new TestResult(skippedResult.TestCase)
            {
                ComputerName = skippedResult.ComputerName,
                DisplayName = skippedResult.DisplayName,
                Outcome = TestOutcome.Failed,
                ErrorMessage = $"{SetUpTestSuiteName}() failed:\n{errorMessage}",
                ErrorStackTrace = errorStackTrace,
                Duration = skippedResult.Duration,
                StandardOutput = skippedResult.StandardOutput
            };
        }

        /// <param name="errorMessage">null if TearDownTestSuite() has not failed</param>
        public static TestResult CreateTearDownTestSuiteResult(TestCase tearDownTestCase, string errorMessage, string errorStackTrace, string output)
        {
            return new TestResult(tearDownTestCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = tearDownTestCase.DisplayName,
                Outcome = errorMessage == null ? TestOutcome.Passed : TestOutcome.Failed,
                ErrorMessage = errorMessage,
                ErrorStackTrace = errorStackTrace,
                Duration = StreamingStandardOutputTestResultParser.ShortTestDuration,
                StandardOutput = string.IsNullOrEmpty(output) ? null : output
            };
        }
    }
}
