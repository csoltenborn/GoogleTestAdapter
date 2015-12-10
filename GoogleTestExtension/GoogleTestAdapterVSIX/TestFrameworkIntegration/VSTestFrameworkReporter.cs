using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

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

        public void ReportTestResults(IFrameworkHandle frameworkHandle, IEnumerable<TestResult2> testResults)
        {
            lock (Lock)
            {
                foreach (TestResult2 testResult in testResults)
                {
                    ReportTestResult(frameworkHandle, testResult);
                }
            }
        }


        private void ReportTestResult(IFrameworkHandle frameworkHandle, TestResult2 testResult)
        {
            TestResult result = testResult.ToTestResult();
            frameworkHandle.RecordResult(result);
            frameworkHandle.RecordEnd(testResult.TestCase, result.Outcome);

            NrOfReportedResults++;
            if (NrOfTestResultsBeforeWaiting != 0 && NrOfReportedResults % NrOfTestResultsBeforeWaiting == 0)
            {
                Thread.Sleep(WaitingTime);
            }
        }

    }

    public static class Extensions
    {
        public static TestResult ToTestResult(this TestResult2 testResult)
        {
            TestResult result = new TestResult(testResult.TestCase);
            result.Outcome = testResult.Outcome.ToTestOutcome();
            result.ComputerName = testResult.ComputerName;
            result.DisplayName = testResult.DisplayName;
            result.Duration = testResult.Duration;
            result.ErrorMessage = testResult.ErrorMessage;
            return result;
        }

        public static TestOutcome ToTestOutcome(this TestOutcome2 testOutcome)
        {
            switch (testOutcome)
            {
                case TestOutcome2.Passed: return TestOutcome.Passed;
                case TestOutcome2.Failed: return TestOutcome.Failed;
                case TestOutcome2.Skipped: return TestOutcome.Skipped;
                case TestOutcome2.None: return TestOutcome.None;
                case TestOutcome2.NotFound: return TestOutcome.NotFound;
                default:
                    throw new Exception();
            }
        }
    }

}