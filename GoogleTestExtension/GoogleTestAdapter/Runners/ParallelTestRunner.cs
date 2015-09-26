using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Runners
{
    class ParallelTestRunner : ITestRunner
    {
        private TestEnvironment TestEnvironment { get; }
        private List<ITestRunner> TestRunners { get; } = new List<ITestRunner>();

        internal ParallelTestRunner(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }

        void ITestRunner.RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle)
        {
            List<Thread> threads;
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            threads = new List<Thread>();
            RunTests(allTestCases, testCasesToRun, threads, runContext, handle);

            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

        void ITestRunner.Cancel()
        {
            foreach (ITestRunner runner in TestRunners)
            {
                runner.Cancel();
            }
        }

        private void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, List<Thread> threads, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            TestEnvironment.LogInfo("GTA: Executing tests on " + splittedTestCasesToRun.Count + " threads");
            TestEnvironment.LogInfo("GTA: Note that no test output will be shown on the test console when executing tests concurrently!",
                TestEnvironment.LogType.UserDebug);

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                ITestRunner runner = new PreparingTestRunner(threadId++, TestEnvironment);
                Thread thread = new Thread(() => runner.RunTests(false, allTestCases, testcases, null, runContext, handle));
                thread.Start();
                threads.Add(thread);
                TestRunners.Add(runner);
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
                TestEnvironment.LogInfo("GTA: Using splitter based on number of tests", TestEnvironment.LogType.UserDebug);
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, TestEnvironment);
                TestEnvironment.LogInfo("GTA: Using splitter based on test durations", TestEnvironment.LogType.UserDebug);
            }

            return splitter;
        }

    }

}