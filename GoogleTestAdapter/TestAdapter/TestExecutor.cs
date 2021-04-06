// This file has been modified by Microsoft on 1/2021.

using System;
using System.Linq;
using System.Collections.Generic;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using ExtensionUriAttribute = Microsoft.VisualStudio.TestPlatform.ObjectModel.ExtensionUriAttribute;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestAdapter.Helpers;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public partial class TestExecutor : ITestExecutor2
    {
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private readonly object _lock = new object();

        private ILogger _logger;
        private SettingsWrapper _settings;
        private GoogleTestExecutor _executor;

        private bool _canceled;

        // ReSharper disable once UnusedMember.Global
        public TestExecutor() : this(null, null) { }

        public TestExecutor(ILogger logger, SettingsWrapper settings)
        {
            _logger = logger;
            _settings = settings;
        }


        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                TryRunTests(executables, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                _logger.LogError(String.Format(Resources.TestRunningException, e));
            }

            CommonFunctions.ReportErrors(_logger, TestPhase.TestExecution, _settings.DebugMode);
        }

        public void RunTests(IEnumerable<VsTestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                TryRunTests(vsTestCasesToRun, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                _logger.LogError(String.Format(Resources.TestRunningException, e));
            }

            CommonFunctions.ReportErrors(_logger, TestPhase.TestExecution, _settings.DebugMode);
        }

        public void Cancel()
        {
            lock (_lock)
            {
                if (_canceled)
                    return;

                _canceled = true;
                _executor?.Cancel();
                _logger.LogInfo(Resources.TestExecutionCancelled);
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
            _logger.LogInfo(String.Format(Resources.TestExecutionCompleted, stopwatch.Elapsed));
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
            _logger.LogInfo(String.Format(Resources.TestExecutionCompleted, stopwatch.Elapsed));
        }

        private Stopwatch StartStopWatchAndInitEnvironment(IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            InitOrRefreshEnvironment(runContext.RunSettings, frameworkHandle);

            CommonFunctions.LogVisualStudioVersion(_logger);

            _logger.LogInfo(CommonResources.TestExecutionStarting);
            _logger.DebugInfo(String.Format(Resources.Settings, _settings));

            return stopwatch;
        }

        private void InitOrRefreshEnvironment(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            if (_settings == null || _settings.GetType() == typeof(SettingsWrapper)) // the latter prevents test settings and logger from being replaced 
                CommonFunctions.CreateEnvironment(runSettings, messageLogger, out _logger, out _settings);
        }

        private bool AbleToRun(IRunContext runContext)
        {
            if (!IsVisualStudioProcessAvailable() && runContext.IsBeingDebugged)
            {
                _logger.LogError(Resources.DebuggingMessage);
                return false;
            }

            return true;
        }

        private IEnumerable<TestCase> GetAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            var allTestCasesInExecutables = new List<TestCase>();

            var discoveryActions = executables
                .OrderBy(e => e)
                .Select(executable => (Action) (() => AddTestCasesOfExecutable(allTestCasesInExecutables, executable, _settings.Clone(), _logger, () => _canceled)))
                .ToArray();
            Utils.SpawnAndWait(discoveryActions);

            if (_canceled)
                allTestCasesInExecutables.Clear();
            
            return allTestCasesInExecutables;
        }

        private static void AddTestCasesOfExecutable(List<TestCase> allTestCasesInExecutables, string executable, SettingsWrapper settings, ILogger logger, Func<bool> testrunIsCanceled)
        {
            if (testrunIsCanceled())
                return;

            if (!GoogleTestDiscoverer.IsGoogleTestExecutable(executable, settings.TestDiscoveryRegex, logger))
                return;

            var discoverer = new GoogleTestDiscoverer(logger, settings);

            settings.ExecuteWithSettingsForExecutable(executable, () =>
            {
                allTestCasesInExecutables.AddRange(discoverer.GetTestsFromExecutable(executable));
            }, logger);
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
                return;

            bool isRunningInsideVisualStudio = !string.IsNullOrEmpty(runContext.SolutionDirectory);
            var reporter = new VsTestFrameworkReporter(frameworkHandle, isRunningInsideVisualStudio, _logger);
            var launcher = new DebuggedProcessLauncher(frameworkHandle);
            ProcessExecutor processExecutor = null;
            if (_settings.UseNewTestExecutionFramework)
            {
                IDebuggerAttacher debuggerAttacher = null;
                if (runContext.IsBeingDebugged)
                    debuggerAttacher = new MessageBasedDebuggerAttacher(_settings.DebuggingNamedPipeId, _logger);
                processExecutor = new ProcessExecutor(debuggerAttacher, _logger);
            }
            lock (_lock)
            {
                if (_canceled)
                    return;

                _executor = new GoogleTestExecutor(_logger, _settings);
            }
            _executor.RunTests(testCasesToRun, reporter, launcher,
                runContext.IsBeingDebugged, runContext.SolutionDirectory, processExecutor);
            reporter.AllTestsFinished();
        }

        public bool ShouldAttachToTestHost(IEnumerable<string> sources, IRunContext runContext)
        {
            // TODO: expose setting in runContext to attach to testhost if needed?
            return false;
        }

        public bool ShouldAttachToTestHost(IEnumerable<VsTestCase> tests, IRunContext runContext)
        {
            // TODO: expose setting in runContext to attach to testhost if needed?
            return false;
        }
    }

}