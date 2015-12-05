using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{
    public class VsTestFrameworkReporter : ITestFrameworkReporter
    {
        private static readonly object Lock = new object();

        private static int NrOfReportedResults { get; set; } = 0;


        private TestEnvironment TestEnvironment { get; }
        private int NrOfTestResultsBeforeWaiting { get; }
        private const int WaitingTime = 1;


        public VsTestFrameworkReporter(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
            NrOfTestResultsBeforeWaiting = TestEnvironment.Options.ReportWaitPeriod;
        }


        public void ReportTestsFound(ITestCaseDiscoverySink sink, IEnumerable<TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase testCase in testCases)
                {
                    sink.SendTestCase(testCase);
                }
            }
        }

        public void ReportTestsStarted(IFrameworkHandle frameworkHandle, IEnumerable<TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase testCase in testCases)
                {
                    frameworkHandle.RecordStart(testCase);
                }
            }
        }

        public void ReportTestResults(IFrameworkHandle frameworkHandle, IEnumerable<TestResult> testResults)
        {
            lock (Lock)
            {
                foreach (TestResult testResult in testResults)
                {
                    ReportTestResult(frameworkHandle, testResult);
                }
            }
        }


        private void ReportTestResult(IFrameworkHandle frameworkHandle, TestResult testResult)
        {
            frameworkHandle.RecordResult(testResult);
            frameworkHandle.RecordEnd(testResult.TestCase, testResult.Outcome);

            NrOfReportedResults++;
            if (NrOfTestResultsBeforeWaiting != 0 && NrOfReportedResults % NrOfTestResultsBeforeWaiting == 0)
            {
                Thread.Sleep(WaitingTime);
            }
        }

    }

}