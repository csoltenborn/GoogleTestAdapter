using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Execution
{
    class ParallelTestRunner : AbstractGoogleTestAdapterClass, IGoogleTestRunner
    {
        internal ParallelTestRunner(AbstractOptions options) : base(options) { }

        private List<IGoogleTestRunner> TestRunners { get; } = new List<IGoogleTestRunner>();

        void IGoogleTestRunner.RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle, string userParameters)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            List<Thread> threads = new List<Thread>();
            lock (this)
            {
                DoRunTests(allTestCases, testCasesToRun, runContext, handle, threads);
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

        void IGoogleTestRunner.Cancel()
        {
            lock (this)
            {
                foreach (IGoogleTestRunner runner in TestRunners)
                {
                    runner.Cancel();
                }
            }
        }

        private void DoRunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle, List<Thread> threads)
        {
            TestDurationSerializer serializer = new TestDurationSerializer();
            TestCase[] testcasesToRun = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testcasesToRun);

            ITestsSplitter splitter = GetTestsSplitter(handle, testcasesToRun, durations);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            handle.SendMessage(TestMessageLevel.Informational, "GTA: Executing tests on " + splittedTestCasesToRun.Count + " threads");
            DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Note that no test output will be shown on the test console when executing tests concurrently!");

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                IGoogleTestRunner runner = new PreparingTestRunner(new SequentialTestRunner(Options), Options, threadId++);
                Thread thread = new Thread(() => runner.RunTests(false, allTestCases, testcases, runContext, handle, null));
                thread.Start();
                threads.Add(thread);
                TestRunners.Add(runner);
            }
        }

        private ITestsSplitter GetTestsSplitter(IFrameworkHandle handle, TestCase[] testCasesToRun, IDictionary<TestCase, int> durations)
        {
            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun);
                DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Using splitter based on number of tests");
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations);
                DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Using splitter based on test durations");
            }

            return splitter;
        }

    }

}