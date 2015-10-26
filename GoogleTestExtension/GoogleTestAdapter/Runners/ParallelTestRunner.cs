using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;

namespace GoogleTestAdapter.Runners
{
    public class ParallelTestRunner : ITestRunner
    {
        private TestEnvironment TestEnvironment { get; }
        private List<ITestRunner> TestRunners { get; } = new List<ITestRunner>();


        public ParallelTestRunner(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
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


        private void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, List<Thread> threads, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            TestEnvironment.LogInfo("Executing tests on " + splittedTestCasesToRun.Count + " threads");
            TestEnvironment.LogInfo("Note that no test output will be shown on the test console when executing tests concurrently!",
                TestEnvironment.LogType.UserDebug);

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                ITestRunner runner = new PreparingTestRunner(threadId++, TestEnvironment);
                TestRunners.Add(runner);

                Thread thread = new Thread(() => runner.RunTests(allTestCases, testcases, null, runContext, handle));
                threads.Add(thread);

                thread.Start();
            }
        }

        private ITestsSplitter GetTestsSplitter(TestCase[] testCasesToRun)
        {
            TestDurationSerializer serializer = new TestDurationSerializer(TestEnvironment);
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testCasesToRun);

            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun, TestEnvironment);
                TestEnvironment.LogInfo("Using splitter based on number of tests", TestEnvironment.LogType.UserDebug);
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, TestEnvironment);
                TestEnvironment.LogInfo("Using splitter based on test durations", TestEnvironment.LogType.UserDebug);
            }

            return splitter;
        }

    }

}