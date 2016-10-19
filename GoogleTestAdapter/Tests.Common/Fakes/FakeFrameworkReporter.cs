using System.Collections.Generic;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Tests.Common.Fakes
{
    public class FakeFrameworkReporter : ITestFrameworkReporter
    {
        public IList<TestResult> ReportedTestResults { get; } = new List<TestResult>();

        public void ReportTestsFound(IEnumerable<TestCase> testCases)
        {
        }

        public void ReportTestsStarted(IEnumerable<TestCase> testCases)
        {
        }

        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            ((List<TestResult>)ReportedTestResults).AddRange(testResults);
        }
    }

}