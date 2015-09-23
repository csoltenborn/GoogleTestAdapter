using System.Collections.Generic;
using System.Threading;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    class VsTestFrameworkReporter
    {
        private readonly int NrOfTestResultsBeforeWaiting;
        private const int WaitingTime = 1;

        private static readonly object Lock = new object();

        private static int NrOfReportedResults { get; set; } = 0;

        internal VsTestFrameworkReporter(AbstractOptions options, IMessageLogger logger)
        {
            NrOfTestResultsBeforeWaiting = options.TestCounter;
            if (NrOfTestResultsBeforeWaiting < 0)
            {
                NrOfTestResultsBeforeWaiting = 1;
                logger.SendMessage(TestMessageLevel.Warning, "Test counter must be >= 0 - using 1 instead");
            }
            DebugUtils.LogUserDebugMessage(logger, options, TestMessageLevel.Informational, "GTA: Test counter is " + NrOfTestResultsBeforeWaiting);
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