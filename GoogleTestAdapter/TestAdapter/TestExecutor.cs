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
using GoogleTestAdapter.TestAdapter.Settings;

namespace GoogleTestAdapter.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://GoogleTestRunner/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        private ILogger _logger;
        private SettingsWrapper _settings;

        private bool _canceled;
        private GoogleTestExecutor _executor;

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
                Stopwatch stopwatch = Stopwatch.StartNew();

                InitOrRefreshEnvironment(runContext.RunSettings, frameworkHandle);

                IList<TestCase> allTestCasesInExecutables = GetAllTestCasesInExecutables(executables).ToList();
                
                ISet<string> allTraitNames = GetAllTraitNames(allTestCasesInExecutables);
                var filter = new TestCaseFilter(runContext, allTraitNames, _logger);
                List<VsTestCase> vsTestCasesToRun =
                    filter.Filter(allTestCasesInExecutables.Select(DataConversionExtensions.ToVsTestCase)).ToList();
                ICollection<TestCase> testCasesToRun =
                    allTestCasesInExecutables.Where(tc => vsTestCasesToRun.Any(vtc => tc.FullyQualifiedName == vtc.FullyQualifiedName)).ToArray();

                DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle);

                stopwatch.Stop();
                _logger.LogInfo($"Google Test execution completed, overall duration: {stopwatch.Elapsed}.");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<VsTestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                InitOrRefreshEnvironment(runContext.RunSettings, frameworkHandle);

                var vsTestCasesToRunAsArray = vsTestCasesToRun as VsTestCase[] ?? vsTestCasesToRun.ToArray();
                ISet<string> allTraitNames = GetAllTraitNames(vsTestCasesToRunAsArray.Select(DataConversionExtensions.ToTestCase));
                var filter = new TestCaseFilter(runContext, allTraitNames, _logger);
                vsTestCasesToRun = filter.Filter(vsTestCasesToRunAsArray);

                IEnumerable<TestCase> allTestCasesInExecutables =
                    GetAllTestCasesInExecutables(vsTestCasesToRun.Select(tc => tc.Source).Distinct());

                ICollection<TestCase> testCasesToRun = vsTestCasesToRun.Select(DataConversionExtensions.ToTestCase).ToArray();
                DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle);

                stopwatch.Stop();
                _logger.LogInfo($"Google Test execution completed, overall duration: {stopwatch.Elapsed}.");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception while running tests: " + e);
            }
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

        internal static void CreateEnvironment(IRunSettings runSettings, IMessageLogger messageLogger, out ILogger logger, out SettingsWrapper settings)
        {
            var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
            RunSettingsContainer ourRunSettings = settingsProvider != null ? settingsProvider.SettingsContainer : new RunSettingsContainer();
            var settingsWrapper = new SettingsWrapper(ourRunSettings);

            var loggerAdapter = new VsTestFrameworkLogger(messageLogger, () => settingsWrapper.DebugMode, () => settingsWrapper.TimestampOutput);
            var regexParser = new RegexTraitParser(loggerAdapter);
            settingsWrapper.RegexTraitParser = regexParser;

            settings = settingsWrapper;
            logger = loggerAdapter;

            logger.DebugInfo($"Solution settings: {settings}");
        }

        private void InitOrRefreshEnvironment(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            if (_settings == null || _settings.GetType() == typeof(SettingsWrapper))
                CreateEnvironment(runSettings, messageLogger, out _logger, out _settings);
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
            var reporter = new VsTestFrameworkReporter(frameworkHandle, isRunningInsideVisualStudio);
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