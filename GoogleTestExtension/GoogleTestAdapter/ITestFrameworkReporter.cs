using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter
{
    public interface ITestFrameworkReporter
    {
        void ReportTestsFound(IEnumerable<TestCase2> testCases);
        void ReportTestsStarted(IEnumerable<TestCase2> testCases);
        void ReportTestResults(IEnumerable<TestResult2> testResults);
    }
}