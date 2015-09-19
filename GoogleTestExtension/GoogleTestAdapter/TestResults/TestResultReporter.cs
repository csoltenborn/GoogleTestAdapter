using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestResults
{
    class TestResultReporter
    {
        private const int NrOfTestResultsBeforeWaiting = 3;
        private const int WaitingTime = 1;

        private static readonly object Lock = new object();

        private IFrameworkHandle FrameworkHandle { get; }
        private static int NrOfReportedResults { get; set; } = 0;

        internal TestResultReporter(IFrameworkHandle frameworkHandle)
        {
            this.FrameworkHandle = frameworkHandle;
        }

        internal void ReportTestsStarted(IEnumerable<TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase testCase in testCases)
                {
                    FrameworkHandle.RecordStart(testCase);
                }
            }
        }

        internal void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            lock (Lock)
            {
                foreach (TestResult testResult in testResults)
                {
                    ReportTestResult(testResult);
                }
            }
        }

        private void ReportTestResult(TestResult testResult)
        {
            FrameworkHandle.RecordResult(testResult);
            FrameworkHandle.RecordEnd(testResult.TestCase, testResult.Outcome);

            NrOfReportedResults++;
            if (NrOfReportedResults % NrOfTestResultsBeforeWaiting == 0)
            {
                Thread.Sleep(WaitingTime);
            }
        }

    }

}