using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class VsTestFrameworkReporter : ITestFrameworkReporter
    {
        private static readonly object Lock = new object();

        private IFrameworkHandle FrameworkHandle { get; }
        private ITestCaseDiscoverySink Sink { get; }

        private Throttle Throttle { get; }
        private bool IsRunningInsideVisualStudio { get; }

        public VsTestFrameworkReporter(ITestCaseDiscoverySink sink) : this(sink, null, false) { }

        public VsTestFrameworkReporter(IFrameworkHandle frameworkHandle, bool isRunningInsideVisualStudio) : this(null, frameworkHandle, isRunningInsideVisualStudio) { }

        private VsTestFrameworkReporter(ITestCaseDiscoverySink sink, IFrameworkHandle frameworkHandle, bool isRunningInsideVisualStudio)
        {
            Sink = sink;
            FrameworkHandle = frameworkHandle;
            IsRunningInsideVisualStudio = isRunningInsideVisualStudio;

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
            Throttle = new Throttle(99, TimeSpan.FromMilliseconds(500));
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
                foreach (TestCase testCase in testCases)
                {
                    FrameworkHandle.RecordStart(DataConversionExtensions.ToVsTestCase(testCase));
                }
            }
        }

        public void ReportTestResults(IEnumerable<TestResult> testResults)
        {
            lock (Lock)
            {
                foreach (TestResult testResult in testResults)
                {
                    if (IsRunningInsideVisualStudio && (testResult.Outcome == TestOutcome.Failed || testResult.Outcome == TestOutcome.Skipped))
                        testResult.ErrorMessage = Environment.NewLine + testResult.ErrorMessage;
                    if (!IsRunningInsideVisualStudio && testResult.ErrorStackTrace != null)
                        testResult.ErrorStackTrace = testResult.ErrorStackTrace.Trim();

                    ReportTestResult(testResult);
                }
            }
        }


        private void ReportTestResult(Model.TestResult testResult)
        {
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult result = testResult.ToVsTestResult();
            Throttle.Execute(delegate
            {
                // This is part of a workaround for a Visual Studio bug. See above.
                FrameworkHandle.RecordResult(result);
            });
            FrameworkHandle.RecordEnd(result.TestCase, result.Outcome);
        }

        internal void AllTestsFinished()
        {
            // This is part of a workaround for a Visual Studio bug. See above.
            bool done = false;
            ThreadPool.QueueUserWorkItem(delegate { Thread.Sleep(1000); done = true; });
            while (!done)
                Thread.Sleep(200);
        }
    }

}