using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter
{
    public class TestResultReporter
    {
        private const int NR_OF_TEST_RESULTS_BEFORE_WAITING = 1;
        private const int WAITING_TIME = 1;

        private static readonly object LOCK = new object();

        private readonly IFrameworkHandle FrameworkHandle;
        private int NrOfReportedResults = 0;

        public TestResultReporter(IFrameworkHandle frameworkHandle)
        {
            this.FrameworkHandle = frameworkHandle;
        }

        private void ReportTestResult(TestResult testResult)
        {
            FrameworkHandle.RecordResult(testResult);
            FrameworkHandle.RecordEnd(testResult.TestCase, testResult.Outcome);

            NrOfReportedResults++;
            if (NrOfReportedResults % NR_OF_TEST_RESULTS_BEFORE_WAITING == 0)
            {
                Thread.Sleep(WAITING_TIME);
            }
        }

        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            lock(LOCK)
            {
                foreach (TestResult testResult in testResults)
                {
                    ReportTestResult(testResult);
                }
            }
        }

    }

}