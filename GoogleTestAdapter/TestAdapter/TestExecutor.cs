// This file has been modified by Microsoft on 6/2017.

using System;
using System.Linq;
using System.Collections.Generic;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using ExtensionUriAttribute = Microsoft.VisualStudio.TestPlatform.ObjectModel.ExtensionUriAttribute;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestAdapter.Helpers;
using GoogleTestAdapter.TestAdapter.Framework;

namespace GoogleTestAdapter.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public partial class TestExecutor : ITestExecutor
    {
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

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
                _logger.LogError($"Exception while running tests: {e}");
            }

            CommonFunctions.ReportErrors(_logger, "test execution", _settings.DebugMode);
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

            CommonFunctions.ReportErrors(_logger, "test execution", _settings.DebugMode);
        }

        public void Cancel()
        {
            lock (this)
            {
                _canceled = true;
                _executor?.Cancel();
                _logger.LogInfo("Test execution canceled.");
            }
        }

        private void TryRunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var stopwatch = StartStopWatchAndInitEnvironment(runContext, frameworkHandle);

            IList<TestCase> allTestCasesInExecutables = GetAllTestCasesInExecutables(executables).ToList();

            ISet<string> allTraitNames = GetAllTraitNames(allTestCasesInExecutables);
            var filter = new TestCaseFilter(runContext, allTraitNames, _logger);
            List<VsTestCase> vsTestCasesToRun =
                filter.Filter(allTestCasesInExecutables.Select(tc => tc.ToVsTestCase())).ToList();
            ICollection<TestCase> testCasesToRun =
                allTestCasesInExecutables.Where(
                    tc => vsTestCasesToRun.Any(vtc => tc.FullyQualifiedName == vtc.FullyQualifiedName)).ToArray();

            DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle);

            stopwatch.Stop();
            _logger.LogInfo($"Google Test execution completed, overall duration: {stopwatch.Elapsed}.");
        }

        private void TryRunTests(IEnumerable<VsTestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var stopwatch = StartStopWatchAndInitEnvironment(runContext, frameworkHandle);

            var vsTestCasesToRunAsArray = vsTestCasesToRun as VsTestCase[] ?? vsTestCasesToRun.ToArray();
            ISet<string> allTraitNames = GetAllTraitNames(vsTestCasesToRunAsArray.Select(tc => tc.ToTestCase()));
            var filter = new TestCaseFilter(runContext, allTraitNames, _logger);
            vsTestCasesToRun = filter.Filter(vsTestCasesToRunAsArray);

            IEnumerable<TestCase> allTestCasesInExecutables =
                GetAllTestCasesInExecutables(vsTestCasesToRun.Select(tc => tc.Source).Distinct());

            ICollection<TestCase> testCasesToRun = vsTestCasesToRun.Select(tc => tc.ToTestCase()).ToArray();
            DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle);

            stopwatch.Stop();
            _logger.LogInfo($"Google Test execution completed, overall duration: {stopwatch.Elapsed}.");
        }

        private Stopwatch StartStopWatchAndInitEnvironment(IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            InitOrRefreshEnvironment(runContext.RunSettings, frameworkHandle);

            CommonFunctions.LogVisualStudioVersion(_logger);

            _logger.LogInfo(Strings.Instance.TestExecutionStarting);
            _logger.DebugInfo($"Solution settings: {_settings}");

            return stopwatch;
        }

        private void InitOrRefreshEnvironment(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            if (_settings == null || _settings.GetType() == typeof(SettingsWrapper)) // the latter prevents test settings and logger from being replaced 
                CommonFunctions.CreateEnvironment(runSettings, messageLogger, out _logger, out _settings);
        }

        private IEnumerable<TestCase> GetAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            var allTestCasesInExecutables = new List<TestCase>();

            var discoverer = new GoogleTestDiscoverer(_logger, _settings);
            foreach (string executable in executables.OrderBy(e => e))
            {
                if (_canceled)
                {
                    allTestCasesInExecutables.Clear();
                    break;
                }

                _settings.ExecuteWithSettingsForExecutable(executable, () =>
                {
                    allTestCasesInExecutables.AddRange(discoverer.GetTestsFromExecutable(executable));
                }, _logger);
            }
            
            return allTestCasesInExecutables;
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

        private void DoRunTests(
            IEnumerable<TestCase> allTestCasesInExecutables, ICollection<TestCase> testCasesToRun,
            IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            bool isRunningInsideVisualStudio = !string.IsNullOrEmpty(runContext.SolutionDirectory);
            var reporter = new VsTestFrameworkReporter(frameworkHandle, isRunningInsideVisualStudio, _logger);
            var launcher = new DebuggedProcessLauncher(frameworkHandle);
            ProcessExecutor processExecutor = null;
            if (_settings.UseNewTestExecutionFramework)
            {
                IDebuggerAttacher debuggerAttacher = null;
                if (runContext.IsBeingDebugged)
                    debuggerAttacher = new VsDebuggerAttacher(_logger, _settings.VisualStudioProcessId);
                processExecutor = new ProcessExecutor(debuggerAttacher, _logger);
            }
            _executor = new GoogleTestExecutor(_logger, _settings);
            _executor.RunTests(allTestCasesInExecutables, testCasesToRun, reporter, launcher,
                runContext.IsBeingDebugged, runContext.SolutionDirectory, processExecutor);
            reporter.AllTestsFinished();
        }

    }

}