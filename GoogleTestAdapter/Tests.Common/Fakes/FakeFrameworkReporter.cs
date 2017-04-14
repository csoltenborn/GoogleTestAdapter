using System.Collections.Generic;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Tests.Common.Fakes
{
    public class FakeFrameworkReporter : ITestFrameworkReporter
    {
        public IList<TestResult> ReportedTestResults { get; } = new List<TestResult>();

        public void ReportTestFound(TestCase testCase)
        {
        }

        public void ReportTestStarted(TestCase testCase)
        {
        }

        public void ReportTestResult(TestResult testResult)
        {
            ReportedTestResults.Add(testResult);
        }
    }

}