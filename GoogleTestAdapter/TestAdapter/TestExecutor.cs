using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
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

        private TestEnvironment _testEnvironment;

        private bool _canceled;
        private GoogleTestExecutor _executor;

        // ReSharper disable once UnusedMember.Global
        public TestExecutor() : this(null) { }

        public TestExecutor(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                InitTestEnvironment(runContext.RunSettings, frameworkHandle);

                IList<Model.TestCase> allTestCasesInExecutables = GetAllTestCasesInExecutables(executables).ToList();

                ISet<string> allTraitNames = GetAllTraitNames(allTestCasesInExecutables);
                var filter = new TestCaseFilter(runContext, allTraitNames, _testEnvironment);
                List<TestCase> vsTestCasesToRun =
                    filter.Filter(allTestCasesInExecutables.Select(DataConversionExtensions.ToVsTestCase)).ToList();
                ICollection<Model.TestCase> testCasesToRun =
                    allTestCasesInExecutables.Where(tc => vsTestCasesToRun.Any(vtc => tc.FullyQualifiedName == vtc.FullyQualifiedName)).ToArray();

                DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle, stopwatch);
            }
            catch (Exception e)
            {
                _testEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<TestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                InitTestEnvironment(runContext.RunSettings, frameworkHandle);

                var vsTestCasesToRunAsArray = vsTestCasesToRun as TestCase[] ?? vsTestCasesToRun.ToArray();
                ISet<string> allTraitNames = GetAllTraitNames(vsTestCasesToRunAsArray.Select(DataConversionExtensions.ToTestCase));
                var filter = new TestCaseFilter(runContext, allTraitNames, _testEnvironment);
                vsTestCasesToRun = filter.Filter(vsTestCasesToRunAsArray);

                IEnumerable<Model.TestCase> allTestCasesInExecutables =
                    GetAllTestCasesInExecutables(vsTestCasesToRun.Select(tc => tc.Source).Distinct());

                ICollection<Model.TestCase> testCasesToRun = vsTestCasesToRun.Select(DataConversionExtensions.ToTestCase).ToArray();
                DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle, stopwatch);
            }
            catch (Exception e)
            {
                _testEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            lock (this)
            {
                _canceled = true;
                _executor?.Cancel();
                _testEnvironment.LogInfo("Test execution canceled.");
            }
        }


        private void InitTestEnvironment(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            if (_testEnvironment == null || _testEnvironment.Options.GetType() == typeof(SettingsWrapper))
            {
                var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                RunSettings ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();
                var settingsWrapper = new SettingsWrapper(ourRunSettings);
                var loggerAdapter = new VsTestFrameworkLogger(messageLogger, settingsWrapper);
                _testEnvironment = new TestEnvironment(settingsWrapper, loggerAdapter);
                settingsWrapper.RegexTraitParser = new RegexTraitParser(_testEnvironment);
            }
        }

        private IEnumerable<Model.TestCase> GetAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            var allTestCasesInExecutables = new List<Model.TestCase>();

            var discoverer = new GoogleTestDiscoverer(_testEnvironment);
            foreach (string executable in executables.OrderBy(e => e))
            {
                if (_canceled)
                {
                    allTestCasesInExecutables.Clear();
                    break;
                }

                allTestCasesInExecutables.AddRange(discoverer.GetTestsFromExecutable(executable));
            }

            return allTestCasesInExecutables;
        }

        private ISet<string> GetAllTraitNames(IEnumerable<Model.TestCase> testCases)
        {
            var allTraitNames = new HashSet<string>();
            foreach (Model.TestCase testCase in testCases)
            {
                foreach (Model.Trait trait in testCase.Traits)
                {
                    allTraitNames.Add(trait.Name);
                }
            }
            return allTraitNames;
        }

        private void DoRunTests(
            IEnumerable<Model.TestCase> allTestCasesInExecutables, ICollection<Model.TestCase> testCasesToRun,
            IRunContext runContext, IFrameworkHandle frameworkHandle, Stopwatch stopwatch)
        {
            bool isRunningInsideVisualStudio = !string.IsNullOrEmpty(runContext.SolutionDirectory);
            var reporter = new VsTestFrameworkReporter(frameworkHandle, isRunningInsideVisualStudio);
            var launcher = new DebuggedProcessLauncher(frameworkHandle);
            _executor = new GoogleTestExecutor(_testEnvironment);
            _executor.RunTests(allTestCasesInExecutables, testCasesToRun, reporter, launcher,
                runContext.IsBeingDebugged, runContext.SolutionDirectory);
            reporter.AllTestsFinished();

            stopwatch.Stop();
            _testEnvironment.LogInfo($"Test execution completed, overall duration: {stopwatch.Elapsed}.");
        }

    }

}