using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Runners
{
    public class ParallelTestRunner : ITestRunner
    {
        private ITestFrameworkReporter FrameworkReporter { get; }
        private TestEnvironment TestEnvironment { get; }
        private List<ITestRunner> TestRunners { get; } = new List<ITestRunner>();


        public ParallelTestRunner(ITestFrameworkReporter reporter, TestEnvironment testEnvironment)
        {
            FrameworkReporter = reporter;
            TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<TestCase2> allTestCases, IEnumerable<TestCase2> testCasesToRun,
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

        public void Cancel()
        {
            lock (this)
            {
                foreach (ITestRunner runner in TestRunners)
                {
                    runner.Cancel();
                }
            }
        }


        private void RunTests(IEnumerable<TestCase2> allTestCases, IEnumerable<TestCase2> testCasesToRun, List<Thread> threads, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase2[] testCasesToRunAsArray = testCasesToRun as TestCase2[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase2>> splittedTestCasesToRun = splitter.SplitTestcases();

            TestEnvironment.LogInfo("Executing tests on " + splittedTestCasesToRun.Count + " threads");
            TestEnvironment.DebugInfo("Note that no test output will be shown on the test console when executing tests concurrently!");

            int threadId = 0;
            foreach (List<TestCase2> testcases in splittedTestCasesToRun)
            {
                ITestRunner runner = new PreparingTestRunner(threadId++, FrameworkReporter, TestEnvironment);
                TestRunners.Add(runner);

                Thread thread = new Thread(() => runner.RunTests(allTestCases, testcases, null, runContext, handle));
                threads.Add(thread);

                thread.Start();
            }
        }

        private ITestsSplitter GetTestsSplitter(TestCase2[] testCasesToRun)
        {
            TestDurationSerializer serializer = new TestDurationSerializer(TestEnvironment);
            IDictionary<TestCase2, int> durations = serializer.ReadTestDurations(testCasesToRun);

            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun, TestEnvironment);
                TestEnvironment.DebugInfo("Using splitter based on number of tests");
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, TestEnvironment);
                TestEnvironment.DebugInfo("Using splitter based on test durations");
            }

            return splitter;
        }

    }

}