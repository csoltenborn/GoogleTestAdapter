using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.VS.Helpers
{
    class VsTestFrameworkReporter : ITestFrameworkReporter
    {
        private static readonly object Lock = new object();

        private static int NrOfReportedResults { get; set; } = 0;

        private IFrameworkHandle FrameworkHandle { get; }
        private ITestCaseDiscoverySink Sink { get; }

        private TestEnvironment TestEnvironment { get; }
        private int NrOfTestResultsBeforeWaiting { get; }
        private const int WaitingTime = 1;


        public VsTestFrameworkReporter(ITestCaseDiscoverySink sink, IFrameworkHandle frameworkHandle, TestEnvironment testEnvironment)
        {
            Sink = sink;
            FrameworkHandle = frameworkHandle;
            TestEnvironment = testEnvironment;
            NrOfTestResultsBeforeWaiting = TestEnvironment.Options.ReportWaitPeriod;
        }


        public void ReportTestsFound(IEnumerable<Model.TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (Model.TestCase testCase in testCases)
                {
                    Sink.SendTestCase(DataConversionExtensions.ToVsTestCase(testCase));
                }
            }
        }

        public void ReportTestsStarted(IEnumerable<Model.TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (Model.TestCase testCase in testCases)
                {
                    FrameworkHandle.RecordStart(DataConversionExtensions.ToVsTestCase(testCase));
                }
            }
        }

        public void ReportTestResults(IEnumerable<Model.TestResult> testResults)
        {
            lock (Lock)
            {
                foreach (Model.TestResult testResult in testResults)
                {
                    ReportTestResult(testResult);
                }
            }
        }


        private void ReportTestResult(Model.TestResult testResult)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult result = testResult.ToVsTestResult();
            FrameworkHandle.RecordResult(result);
            FrameworkHandle.RecordEnd(result.TestCase, result.Outcome);

            NrOfReportedResults++;
            if (NrOfTestResultsBeforeWaiting != 0 && NrOfReportedResults % NrOfTestResultsBeforeWaiting == 0)
            {
                Thread.Sleep(WaitingTime);
            }
        }

    }

}