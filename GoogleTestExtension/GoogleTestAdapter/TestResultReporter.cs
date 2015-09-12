using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter
{
    public class TestResultReporter
    {
        private const int NR_OF_TEST_RESULTS_BEFORE_WAITING = 1;
        private const int WAITING_TIME = 1;

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            foreach (TestResult testResult in testResults)
            {
                ReportTestResult(testResult);
            }
        }

    }

}