using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Runners
{
    public class ParallelTestRunner : ITestRunner
    {
        private ITestFrameworkReporter FrameworkReporter { get; }
        private TestEnvironment TestEnvironment { get; }
        private List<ITestRunner> TestRunners { get; } = new List<ITestRunner>();
        private string SolutionDirectory { get; }


        public ParallelTestRunner(ITestFrameworkReporter reporter, TestEnvironment testEnvironment, string solutionDirectory)
        {
            FrameworkReporter = reporter;
            TestEnvironment = testEnvironment;
            SolutionDirectory = solutionDirectory;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
            string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            List<Thread> threads;
            lock (this)
            {
                DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

                threads = new List<Thread>();
                RunTests(allTestCases, testCasesToRun, baseDir, threads, isBeingDebugged, debuggedLauncher);
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


        private void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, List<Thread> threads, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            TestEnvironment.LogInfo("Executing tests on " + splittedTestCasesToRun.Count + " threads");
            TestEnvironment.DebugInfo("Note that no test output will be shown on the test console when executing tests concurrently!");

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                ITestRunner runner = new PreparingTestRunner(threadId++, SolutionDirectory, FrameworkReporter, TestEnvironment);
                TestRunners.Add(runner);

                Thread thread = new Thread(() => runner.RunTests(allTestCases, testcases, baseDir, null, isBeingDebugged, debuggedLauncher));
                threads.Add(thread);

                thread.Start();
            }
        }

        private ITestsSplitter GetTestsSplitter(TestCase[] testCasesToRun)
        {
            var serializer = new TestDurationSerializer();
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testCasesToRun);

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