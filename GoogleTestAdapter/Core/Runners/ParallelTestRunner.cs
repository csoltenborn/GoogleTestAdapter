﻿// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Runners
{
    public class ParallelTestRunner : ITestRunner
    {
        private readonly ITestFrameworkReporter _frameworkReporter;
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly List<ITestRunner> _testRunners = new List<ITestRunner>();
        private readonly SchedulingAnalyzer _schedulingAnalyzer;


        public ParallelTestRunner(ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
        {
            _frameworkReporter = reporter;
            _logger = logger;
            _settings = settings;
            _schedulingAnalyzer = schedulingAnalyzer;
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, 
            IDebuggedProcessExecutorFactory processExecutorFactory)
        {
            List<Thread> threads;
            lock (this)
            {
                threads = new List<Thread>();
                RunTests(testCasesToRun, threads, isBeingDebugged, processExecutorFactory);
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var result in _testRunners.SelectMany(r => r.ExecutableResults))
            {
                ExecutableResults.Add(result);
            }
        }

        public IList<ExecutableResult> ExecutableResults { get; } = new List<ExecutableResult>();

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


        private void RunTests(IEnumerable<TestCase> testCasesToRun, List<Thread> threads, bool isBeingDebugged, IDebuggedProcessExecutorFactory processExecutorFactory)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();

            ITestsSplitter splitter = GetTestsSplitter(testCasesToRunAsArray);
            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();

            _logger.LogInfo(string.Format(Resources.ThreadExecutionMessage, splittedTestCasesToRun.Count));
            _logger.DebugInfo(Resources.NoTestOutputShown);

            int threadId = 0;
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                var runner = new PreparingTestRunner(threadId++, _frameworkReporter, _logger, _settings.Clone(), _schedulingAnalyzer);
                _testRunners.Add(runner);

                var thread = new Thread(
                    () => runner.RunTests(testcases, isBeingDebugged, processExecutorFactory)){ Name = $"GTA Testrunner {threadId}" };
                threads.Add(thread);

                thread.Start();
            }
        }

        private ITestsSplitter GetTestsSplitter(TestCase[] testCasesToRun)
        {
            IDictionary<TestCase, int> durations = null;
            try
            {
                var serializer = new TestDurationSerializer();
                durations = serializer.ReadTestDurations(testCasesToRun);
                foreach (KeyValuePair<TestCase, int> duration in durations)
                {
                    if (!_schedulingAnalyzer.AddExpectedDuration(duration.Key, duration.Value))
                        _logger.DebugWarning(String.Format(Resources.TestCaseInAnalyzer, duration.Key.FullyQualifiedName));
                }
            }
            catch (InvalidTestDurationsException e)
            {
                _logger.LogWarning(string.Format(Resources.ReadTestDurationError, e.Message));
            }

            ITestsSplitter splitter;
            if (durations == null || durations.Count < testCasesToRun.Length)
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun, _settings);
                _logger.DebugInfo(Resources.UsingSplitterOnNumber);
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations, _settings);
                _logger.DebugInfo(Resources.UsingSplitterOnDuration);
            }

            return splitter;
        }

    }

}