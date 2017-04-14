using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Framework
{

    public interface ITestFrameworkReporter
    {
        void ReportTestFound(TestCase testCase);

        void ReportTestStarted(TestCase testCase);

        /// <exception cref="TestRunCanceledException">if test execution has been canceled in the meantime</exception>
        void ReportTestResult(TestResult testResult);
    }

}