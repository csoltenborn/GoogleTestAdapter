using System;
using System.Collections.Generic;
using System.Linq;
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


        public void ReportTestsFound(ITestCaseDiscoverySink sink, IEnumerable<TestCase2> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase2 testCase in testCases)
                {
                    sink.SendTestCase(Extensions.ToVsTestCase(testCase));
                }
            }
        }

        public void ReportTestsStarted(IFrameworkHandle frameworkHandle, IEnumerable<TestCase2> testCases)
        {
            lock (Lock)
            {
                foreach (TestCase2 testCase in testCases)
                {
                    frameworkHandle.RecordStart(Extensions.ToVsTestCase(testCase));
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
            frameworkHandle.RecordEnd(Extensions.ToVsTestCase(testResult.TestCase), result.Outcome);

            NrOfReportedResults++;
            if (NrOfTestResultsBeforeWaiting != 0 && NrOfReportedResults % NrOfTestResultsBeforeWaiting == 0)
            {
                Thread.Sleep(WaitingTime);
            }
        }

    }

    public static class Extensions
    {
        public static TestCase ToVsTestCase(this TestCase2 testCase)
        {
            TestCase result = new TestCase(testCase.FullyQualifiedName, testCase.ExecutorUri, testCase.Source);
            result.DisplayName = testCase.DisplayName;
            result.CodeFilePath = testCase.CodeFilePath;
            result.LineNumber = testCase.LineNumber;
            result.Traits.AddRange(testCase.Traits.Select(ToVsTrait));
            return result;
        }

        public static TestCase2 ToTestCase(this TestCase testCase)
        {
            TestCase2 result = new TestCase2(testCase.FullyQualifiedName, testCase.ExecutorUri, testCase.Source);
            result.DisplayName = testCase.DisplayName;
            result.CodeFilePath = testCase.CodeFilePath;
            result.LineNumber = testCase.LineNumber;
            result.Traits.AddRange(testCase.Traits.Select(ToTrait));
            return result;
        }

        public static Trait ToVsTrait(this Trait2 trait)
        {
            return new Trait(trait.Name, trait.Value);
        }

        public static Trait2 ToTrait(this Trait trait)
        {
            return new Trait2(trait.Name, trait.Value);
        }

        public static TestResult ToTestResult(this TestResult2 testResult)
        {
            TestResult result = new TestResult(ToVsTestCase(testResult.TestCase));
            result.Outcome = testResult.Outcome.ToVsTestOutcome();
            result.ComputerName = testResult.ComputerName;
            result.DisplayName = testResult.DisplayName;
            result.Duration = testResult.Duration;
            result.ErrorMessage = testResult.ErrorMessage;
            return result;
        }

        public static TestOutcome ToVsTestOutcome(this TestOutcome2 testOutcome)
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