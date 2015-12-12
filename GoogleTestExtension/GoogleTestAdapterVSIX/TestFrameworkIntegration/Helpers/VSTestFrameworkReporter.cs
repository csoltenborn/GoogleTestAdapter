using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter;

namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Helpers
{
    public class VsTestFrameworkReporter : ITestFrameworkReporter
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


        public void ReportTestsFound(IEnumerable<GoogleTestAdapter.Model.TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (GoogleTestAdapter.Model.TestCase testCase in testCases)
                {
                    Sink.SendTestCase((Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase)DataConversionExtensions.ToVsTestCase(testCase));
                }
            }
        }

        public void ReportTestsStarted(IEnumerable<GoogleTestAdapter.Model.TestCase> testCases)
        {
            lock (Lock)
            {
                foreach (GoogleTestAdapter.Model.TestCase testCase in testCases)
                {
                    FrameworkHandle.RecordStart((Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase)DataConversionExtensions.ToVsTestCase(testCase));
                }
            }
        }

        public void ReportTestResults(IEnumerable<GoogleTestAdapter.Model.TestResult> testResults)
        {
            lock (Lock)
            {
                foreach (GoogleTestAdapter.Model.TestResult testResult in testResults)
                {
                    ReportTestResult(testResult);
                }
            }
        }


        private void ReportTestResult(GoogleTestAdapter.Model.TestResult testResult)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult result = testResult.ToTestResult();
            FrameworkHandle.RecordResult(result);
            FrameworkHandle.RecordEnd((Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase)DataConversionExtensions.ToVsTestCase(testResult.TestCase), result.Outcome);

            NrOfReportedResults++;
            if (NrOfTestResultsBeforeWaiting != 0 && NrOfReportedResults % NrOfTestResultsBeforeWaiting == 0)
            {
                Thread.Sleep(WaitingTime);
            }
        }

    }

}