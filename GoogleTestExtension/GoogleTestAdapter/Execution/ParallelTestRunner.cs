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
        public bool Canceled { get; set; }

        internal ParallelTestRunner(AbstractOptions options) : base(options) { }

        public void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle, string testDirectory)
        {
            if (testDirectory != null)
            {
                throw new ArgumentException("testDirectory must be null");
            }

            TestDurationSerializer serializer = new TestDurationSerializer();
            TestCase[] testcasesToRun = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testcasesToRun);
            ITestsSplitter splitter;
            if (durations.Count < testcasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testcasesToRun);
                DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Using splitter based on number of tests");
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations);
                DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "GTA: Using splitter based on test durations");
            }

            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();
            List<Thread> threads = new List<Thread>();
            handle.SendMessage(TestMessageLevel.Informational, "GTA: Executing " + testcasesToRun.Length + " tests on " + splittedTestCasesToRun.Count + " threads");
            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                IGoogleTestRunner runner = new PreparingTestRunner(new SequentialTestRunner(Options), Options, threadId++);
                Thread thread = new Thread(() => runner.RunTests(false, allTestCases, testcases, runContext, handle, null));
                thread.Start();
                threads.Add(thread);
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

    }

}