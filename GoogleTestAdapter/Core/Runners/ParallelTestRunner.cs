using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Runners
{
    public class ParallelTestRunner : ITestRunner
    {
        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly List<ITestRunner> _testRunners = new List<ITestRunner>();
        private readonly string _solutionDirectory;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;


        public ParallelTestRunner(ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, string solutionDirectory, SchedulingAnalyzer schedulingAnalyzer)
        {
            _frameworkReporter = reporter;
            _logger = logger;
            _settings = settings;
            _solutionDirectory = solutionDirectory;
            _schedulingAnalyzer = schedulingAnalyzer;
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, string baseDir,
            string workingDir, string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            List<Thread> threads;
            lock (this)
            {
                DebugUtils.AssertIsNull(workingDir, nameof(workingDir));
                DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

                threads = new List<Thread>();
                RunTests(testCasesToRun, baseDir, threads, isBeingDebugged, debuggedLauncher, executor);
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


        private void RunTests(IEnumerable<TestCase> testCasesToRun, string baseDir, List<Thread> threads, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            _logger.LogInfo("Executing tests on " + splittedTestCasesToRun.Count + " threads");
            _logger.DebugInfo("Note that no test output will be shown on the test console when executing tests concurrently!");

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                var runner = new PreparingTestRunner(threadId++, _solutionDirectory, _frameworkReporter, _logger, _settings.Clone(), _schedulingAnalyzer);
                _testRunners.Add(runner);

                var thread = new Thread(
                    () => runner.RunTests(testcases, baseDir, null, null, isBeingDebugged, debuggedLauncher, executor)){ Name = $"GTA Testrunner {threadId}" };
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
                    _logger.DebugWarning("TestCase already in analyzer: " + duration.Key.FullyQualifiedName);
            }

            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun, _settings);
                _logger.DebugInfo("Using splitter based on number of tests");
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, _settings);
                _logger.DebugInfo("Using splitter based on test durations");
            }

            return splitter;
        }

    }

}