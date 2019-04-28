// This file has been modified by Microsoft on 6/2017.

using System;
using System.Linq;
using System.Collections.Generic;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using ExtensionUriAttribute = Microsoft.VisualStudio.TestPlatform.ObjectModel.ExtensionUriAttribute;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestAdapter.Helpers;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.ProcessExecution;
using GoogleTestAdapter.TestResults;

namespace GoogleTestAdapter.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public partial class TestExecutor : ITestExecutor
    {
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private readonly object _lock = new object();

        private ILogger _logger;
        private SettingsWrapper _settings;
        private readonly IDebuggerAttacher _debuggerAttacher;
        private GoogleTestExecutor _executor;

        private bool _canceled;

        // ReSharper disable once UnusedMember.Global
        public TestExecutor() : this(null, null, null) { }

        public TestExecutor(ILogger logger, SettingsWrapper settings, IDebuggerAttacher debuggerAttacher)
        {
            _logger = logger;
            _settings = settings;
            _debuggerAttacher = debuggerAttacher;
        }


        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                TryRunTests(executables, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while running tests: {e}");
            }

            CommonFunctions.ReportErrors(_logger, "test execution", _settings.OutputMode, _settings.SummaryMode);
        }

        public void RunTests(IEnumerable<VsTestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                TryRunTests(vsTestCasesToRun, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception while running tests: " + e);
            }

            CommonFunctions.ReportErrors(_logger, "test execution", _settings.OutputMode, _settings.SummaryMode);
        }

        public void Cancel()
        {
            lock (_lock)
            {
                if (_canceled)
                    return;

                _canceled = true;
                _executor?.Cancel();
                _logger.LogInfo("Test execution canceled.");
            }
        }

        private void TryRunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var stopwatch = StartStopWatchAndInitEnvironment(runContext, frameworkHandle);

            if (!AbleToRun(runContext))
                return;

            IList<TestCase> allTestCasesInExecutables = GetAllTestCasesInExecutables(executables).ToList();

            ISet<string> allTraitNames = GetAllTraitNames(allTestCasesInExecutables);
            var filter = new TestCaseFilter(runContext, allTraitNames, _logger);
            List<VsTestCase> vsTestCasesToRun =
                filter.Filter(allTestCasesInExecutables.Select(tc => tc.ToVsTestCase())).ToList();
            ICollection<TestCase> testCasesToRun =
                allTestCasesInExecutables.Where(
                    tc => vsTestCasesToRun.Any(vtc => tc.FullyQualifiedName == vtc.FullyQualifiedName)).ToArray();

            DoRunTests(testCasesToRun, runContext, frameworkHandle);

            stopwatch.Stop();
            _logger.LogInfo($"Google Test execution completed, overall duration: {stopwatch.Elapsed}.");
        }

        private void TryRunTests(IEnumerable<VsTestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var stopwatch = StartStopWatchAndInitEnvironment(runContext, frameworkHandle);

            if (!AbleToRun(runContext))
                return;

            var vsTestCasesToRunAsArray = vsTestCasesToRun as VsTestCase[] ?? vsTestCasesToRun.ToArray();
            ISet<string> allTraitNames = GetAllTraitNames(vsTestCasesToRunAsArray.Select(tc => tc.ToTestCase()));
            var filter = new TestCaseFilter(runContext, allTraitNames, _logger);
            vsTestCasesToRun = filter.Filter(vsTestCasesToRunAsArray);

            ICollection<TestCase> testCasesToRun = vsTestCasesToRun.Select(tc => tc.ToTestCase()).ToArray();
            DoRunTests(testCasesToRun, runContext, frameworkHandle);

            stopwatch.Stop();
            _logger.LogInfo($"Google Test execution completed, overall duration: {stopwatch.Elapsed}.");
        }

        private Stopwatch StartStopWatchAndInitEnvironment(IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            InitOrRefreshEnvironment(runContext.RunSettings, frameworkHandle, runContext);

            CommonFunctions.LogVisualStudioVersion(_logger);

            _logger.LogInfo(Strings.Instance.TestExecutionStarting);
            _logger.DebugInfo($"Solution settings: {_settings}");

            return stopwatch;
        }

        private void InitOrRefreshEnvironment(IRunSettings runSettings, IMessageLogger messageLogger, IRunContext runContext)
        {
            if (_settings == null || _settings.GetType() == typeof(SettingsWrapper)) // the latter prevents test settings and logger from being replaced 
                CommonFunctions.CreateEnvironment(runSettings, messageLogger, out _logger, out _settings, runContext.SolutionDirectory);
        }

        private bool AbleToRun(IRunContext runContext)
        {
            if (!IsVisualStudioProcessAvailable() && runContext.IsBeingDebugged)
            {
                _logger.LogError("Debugging is only possible if GoogleTestAdapter has been installed into Visual Studio - NuGet installation does not support this (and other features such as Visual Studio Options, toolbar, and solution settings).");
                return false;
            }

            return true;
        }

        private IEnumerable<TestCase> GetAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            var allTestCasesInExecutables = new List<TestCase>();

            var discoveryActions = executables
                .OrderBy(e => e)
                .Select(executable => (Action) (() =>
                {
                    var testCases = GetTestCasesOfExecutable(executable, _settings.Clone(), _logger, () => _canceled);
                    lock (allTestCasesInExecutables)
                    {
                        allTestCasesInExecutables.AddRange(testCases);
                    }
                }))
                .ToArray();
            Utils.SpawnAndWait(discoveryActions);

            if (_canceled)
                allTestCasesInExecutables.Clear();
            
            return allTestCasesInExecutables;
        }

        private static IList<TestCase> GetTestCasesOfExecutable(string executable, SettingsWrapper settings, ILogger logger, Func<bool> testrunIsCanceled)
        {
            IList<TestCase> testCases = new List<TestCase>();

            if (testrunIsCanceled())
                return testCases;

            var discoverer = new GoogleTestDiscoverer(logger, settings);
            settings.ExecuteWithSettingsForExecutable(executable, logger, () =>
            {
                testCases = discoverer.GetTestsFromExecutable(executable);
            });

            return testCases;
        }

        private ISet<string> GetAllTraitNames(IEnumerable<TestCase> testCases)
        {
            var allTraitNames = new HashSet<string>();
            foreach (TestCase testCase in testCases)
            {
                foreach (Trait trait in testCase.Traits)
                {
                    allTraitNames.Add(trait.Name);
                }
            }
            return allTraitNames;
        }

        private bool IsVisualStudioProcessAvailable()
        {
            return _settings.DebuggingNamedPipeId != null;
        }

        private void DoRunTests(ICollection<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (testCasesToRun.Count == 0)
            {
                return;
            }

            bool isRunningInsideVisualStudio = !string.IsNullOrEmpty(runContext.SolutionDirectory);
            var reporter = new VsTestFrameworkReporter(frameworkHandle, isRunningInsideVisualStudio, _logger);

            var debuggerAttacher = _debuggerAttacher ?? new MessageBasedDebuggerAttacher(_settings.DebuggingNamedPipeId, _logger);
            var processExecutorFactory = new DebuggedProcessExecutorFactory(frameworkHandle, debuggerAttacher);
            var exitCodeTestsAggregator = new ExitCodeTestsAggregator();
            var exitCodeTestsReporter = new ExitCodeTestsReporter(reporter, exitCodeTestsAggregator, _settings, _logger);

            lock (_lock)
            {
                if (_canceled)
                    return;

                _executor = new GoogleTestExecutor(_logger, _settings, processExecutorFactory, exitCodeTestsReporter);
            }
            _executor.RunTests(testCasesToRun, reporter, runContext.IsBeingDebugged);
            reporter.AllTestsFinished();
        }

    }

}