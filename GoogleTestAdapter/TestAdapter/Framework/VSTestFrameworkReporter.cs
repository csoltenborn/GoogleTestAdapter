using System;
using System.Collections.Generic;
using System.Threading;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestAdapter.Helpers;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class VsTestFrameworkReporter : ITestFrameworkReporter
    {
        public const int NrOfTestsBeforeThrottling = 99;
        public const int ThrottleDurationInMs = 500;
        public const int SleepingTimeAfterAllTestsInMs = 1000;

        private static readonly object Lock = new object();

        private readonly ILogger _logger;

        private readonly IFrameworkHandle _frameworkHandle;
        private readonly ITestCaseDiscoverySink _sink;

        private readonly Throttle _throttle;
        private readonly bool _isRunningInsideVisualStudio;

        public VsTestFrameworkReporter(ITestCaseDiscoverySink sink, ILogger logger) : this(sink, null, false, logger) { }

        public VsTestFrameworkReporter(IFrameworkHandle frameworkHandle, bool isRunningInsideVisualStudio, ILogger logger) : this(null, frameworkHandle, isRunningInsideVisualStudio, logger) { }

        private VsTestFrameworkReporter(ITestCaseDiscoverySink sink, IFrameworkHandle frameworkHandle, bool isRunningInsideVisualStudio, ILogger logger)
        {
            _sink = sink;
            _frameworkHandle = frameworkHandle;
            _isRunningInsideVisualStudio = isRunningInsideVisualStudio;
            _logger = logger;

            // This is part of a workaround for a Visual Studio bug (see issue #15).
            // If test results are reported too quickly (100 or more in 500ms), the
            // Visual Studio test framework internally will start new work items in
            // a ThreadPool to process results there. If the TestExecutor returns
            // before all work items in the ThreadPool have been processed, results
            // will be lost. We work around this in two ways:
            //   1) ReportTestResult: Minimize the chance of reporting too quickly
            //      by throttling our own output.
            //   2) AllTestsFinished: We would like to wait till all other ThreadPool
            //      work item have finished. The closest approximation is to add a
            //      work item which will be scheduled after all others and let it
            //      sleep for a short time.
            _throttle = new Throttle(NrOfTestsBeforeThrottling, TimeSpan.FromMilliseconds(ThrottleDurationInMs));
        }


        public void ReportTestsFound(IEnumerable<TestCase> testCases)
        {
            foreach (TestCase testCase in testCases)
            {
                _sink.SendTestCase(testCase.ToVsTestCase());
            }
        }

        public void ReportTestsStarted(IEnumerable<TestCase> testCases)
        {
            foreach (TestCase testCase in testCases)
            {
                _frameworkHandle.RecordStart(testCase.ToVsTestCase());
            }
        }

        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            lock (Lock)
            {
                foreach (TestResult testResult in testResults)
                {
                    if (_isRunningInsideVisualStudio && (testResult.Outcome == TestOutcome.Failed || testResult.Outcome == TestOutcome.Skipped))
                        testResult.ErrorMessage = Environment.NewLine + testResult.ErrorMessage;
                    if (!_isRunningInsideVisualStudio && testResult.ErrorStackTrace != null)
                        testResult.ErrorStackTrace = testResult.ErrorStackTrace.Trim();

                    try
                    {
                        ReportTestResult(testResult);
                    }
                    catch (TestCanceledException e)
                    {
                        throw new TestRunCanceledException($"{nameof(VsTestFrameworkReporter)} caught TestCanceledException", e);
                    }
                }
            }
        }


        private bool TestReportingNeedsToBeThrottled()
        {
            return VsVersionUtils.VsVersion.NeedsToBeThrottled();
        }

        private void ReportTestResult(TestResult testResult)
        {
            VsTestResult result = testResult.ToVsTestResult();

            if (TestReportingNeedsToBeThrottled())
            {
                _throttle.Execute(delegate
                {
                    // This is part of a workaround for a Visual Studio bug. See above.
                    _frameworkHandle.RecordResult(result);
                });
            }
            else
            {
                _frameworkHandle.RecordResult(result);
            }

            _frameworkHandle.RecordEnd(result.TestCase, result.Outcome);
        }

        internal void AllTestsFinished()
        {
            if (TestReportingNeedsToBeThrottled())
            {
                // This is part of a workaround for a Visual Studio bug. See above.
                bool done = false;
                ThreadPool.QueueUserWorkItem(delegate { Thread.Sleep(SleepingTimeAfterAllTestsInMs); done = true; });
                while (!done)
                    Thread.Sleep(200);
            }
        }
    }

}