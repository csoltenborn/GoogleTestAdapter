using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Runners
{
    class ParallelTestRunner : AbstractOptionsProvider, ITestRunner
    {
        internal ParallelTestRunner(AbstractOptions options) : base(options) { }

        private List<ITestRunner> TestRunners { get; } = new List<ITestRunner>();

        void ITestRunner.RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle)
        {
            List<Thread> threads;
            lock (this)
            {
                DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

                threads = new List<Thread>();
                RunTests(allTestCases, testCasesToRun, threads, runContext, handle);
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

        void ITestRunner.Cancel()
        {
            lock (this)
            {
                foreach (ITestRunner runner in TestRunners)
                {
                    runner.Cancel();
                }
            }
        }

        private void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, List<Thread> threads, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray, handle);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            handle.SendMessage(TestMessageLevel.Informational, "GTA: Executing tests on " + splittedTestCasesToRun.Count + " threads");
            DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Note that no test output will be shown on the test console when executing tests concurrently!");

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                ITestRunner runner = new PreparingTestRunner(threadId++, Options);
                Thread thread = new Thread(() => runner.RunTests(false, allTestCases, testcases, null, runContext, handle));
                thread.Start();
                threads.Add(thread);
                TestRunners.Add(runner);
            }
        }

        private ITestsSplitter GetTestsSplitter(TestCase[] testCasesToRun, IMessageLogger logger)
        {
            TestDurationSerializer serializer = new TestDurationSerializer(Options);
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testCasesToRun);

            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun, Options);
                DebugUtils.LogUserDebugMessage(logger, Options, TestMessageLevel.Informational, "GTA: Using splitter based on number of tests");
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, Options);
                DebugUtils.LogUserDebugMessage(logger, Options, TestMessageLevel.Informational, "GTA: Using splitter based on test durations");
            }

            return splitter;
        }

    }

}