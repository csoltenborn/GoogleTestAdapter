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
        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly TestEnvironment _testEnvironment;
        private readonly List<ITestRunner> _testRunners = new List<ITestRunner>();
        private readonly string _solutionDirectory;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;


        public ParallelTestRunner(ITestFrameworkReporter reporter, TestEnvironment testEnvironment, string solutionDirectory, SchedulingAnalyzer schedulingAnalyzer)
        {
            _frameworkReporter = reporter;
            _testEnvironment = testEnvironment;
            _solutionDirectory = solutionDirectory;
            _schedulingAnalyzer = schedulingAnalyzer;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
            string workingDir, string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            List<Thread> threads;
            lock (this)
            {
                DebugUtils.AssertIsNull(workingDir, nameof(workingDir));
                DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

                threads = new List<Thread>();
                RunTests(allTestCases, testCasesToRun, baseDir, threads, isBeingDebugged, debuggedLauncher, executor);
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
                foreach (ITestRunner runner in _testRunners)
                {
                    runner.Cancel();
                }
            }
        }


        private void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir, List<Thread> threads, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            _testEnvironment.Logger.LogInfo("Executing tests on " + splittedTestCasesToRun.Count + " threads");
            _testEnvironment.Logger.DebugInfo("Note that no test output will be shown on the test console when executing tests concurrently!");

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                var threadTestEnvironment = new TestEnvironment(_testEnvironment.Options.Clone(), _testEnvironment.Logger);
                var runner = new PreparingTestRunner(threadId++, _solutionDirectory, _frameworkReporter, threadTestEnvironment, _schedulingAnalyzer);
                _testRunners.Add(runner);

                var thread = new Thread(() => runner.RunTests(allTestCases, testcases, baseDir, null, null, isBeingDebugged, debuggedLauncher, executor));
                threads.Add(thread);

                thread.Start();
            }
        }

        private ITestsSplitter GetTestsSplitter(TestCase[] testCasesToRun)
        {
            var serializer = new TestDurationSerializer();
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testCasesToRun);
            foreach (KeyValuePair<TestCase, int> duration in durations)
            {
                if (!_schedulingAnalyzer.AddExpectedDuration(duration.Key, duration.Value))
                    _testEnvironment.Logger.DebugWarning("TestCase already in analyzer: " + duration.Key.FullyQualifiedName);
            }

            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun, _testEnvironment);
                _testEnvironment.Logger.DebugInfo("Using splitter based on number of tests");
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, _testEnvironment);
                _testEnvironment.Logger.DebugInfo("Using splitter based on test durations");
            }

            return splitter;
        }

    }

}