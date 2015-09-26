using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{
    class VsTestFrameworkReporter
    {
        private static readonly object Lock = new object();

        private static int NrOfReportedResults { get; set; } = 0;

        private TestEnvironment TestEnvironment { get; }
        private readonly int NrOfTestResultsBeforeWaiting;
        private const int WaitingTime = 1;

        internal VsTestFrameworkReporter(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
            NrOfTestResultsBeforeWaiting = TestEnvironment.Options.ReportWaitPeriod;
            if (NrOfTestResultsBeforeWaiting < 0)
            {
                NrOfTestResultsBeforeWaiting = 1;
                TestEnvironment.LogWarning("Test counter must be >= 0 - using 1 instead");
            }
            TestEnvironment.LogInfo("GTA: Test counter is " + NrOfTestResultsBeforeWaiting);
        }

        internal void ReportTestsFound(ITestCaseDiscoverySink sink, IEnumerable<TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase testCase in testCases)
                {
                    sink.SendTestCase(testCase);
                }
            }
        }

        internal void ReportTestsStarted(IFrameworkHandle frameworkHandle, IEnumerable<TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase testCase in testCases)
                {
                    frameworkHandle.RecordStart(testCase);
                }
            }
        }

        internal void ReportTestResults(IFrameworkHandle frameworkHandle, IEnumerable<TestResult> testResults)
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