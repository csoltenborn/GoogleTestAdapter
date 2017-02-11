using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Framework
{

    public interface ITestFrameworkReporter
    {
        void ReportTestsFound(IEnumerable<TestCase> testCases);

        void ReportTestsStarted(IEnumerable<TestCase> testCases);

        /// <exception cref="TestRunCanceledException">if test execution has been canceled in the meantime</exception>
        void ReportTestResults(IEnumerable<TestResult> testResults);
    }

}