using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter
{
    public interface ITestFrameworkReporter
    {
        void ReportTestsFound(IEnumerable<TestCase> testCases);
        void ReportTestsStarted(IEnumerable<TestCase> testCases);
        void ReportTestResults(IEnumerable<TestResult> testResults);
    }
}