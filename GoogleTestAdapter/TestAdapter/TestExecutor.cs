using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Framework;
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

        private TestEnvironment TestEnvironment { get; set; }

        private bool Canceled { get; set; } = false;
        private GoogleTestExecutor Executor { get; set; }

        public TestExecutor() : this(null) { }

        public TestExecutor(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                InitTestEnvironment(runContext.RunSettings, frameworkHandle);

                IList<Model.TestCase> allTestCasesInExecutables = GetAllTestCasesInExecutables(executables).ToList();

                ISet<string> allTraitNames = GetAllTraitNames(allTestCasesInExecutables);
                TestCaseFilter filter = new TestCaseFilter(runContext, allTraitNames, TestEnvironment);
                List<TestCase> vsTestCasesToRun =
                    filter.Filter(allTestCasesInExecutables.Select(DataConversionExtensions.ToVsTestCase)).ToList();
                IEnumerable<Model.TestCase> testCasesToRun =
                    allTestCasesInExecutables.Where(tc => vsTestCasesToRun.Any(vtc => tc.FullyQualifiedName == vtc.FullyQualifiedName)).ToList();

                DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<TestCase> vsTestCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                InitTestEnvironment(runContext.RunSettings, frameworkHandle);

                var vsTestCasesToRunAsArray = vsTestCasesToRun as TestCase[] ?? vsTestCasesToRun.ToArray();
                ISet<string> allTraitNames = GetAllTraitNames(vsTestCasesToRunAsArray.Select(DataConversionExtensions.ToTestCase));
                TestCaseFilter filter = new TestCaseFilter(runContext, allTraitNames, TestEnvironment);
                vsTestCasesToRun = filter.Filter(vsTestCasesToRunAsArray);

                IEnumerable<Model.TestCase> allTestCasesInExecutables =
                    GetAllTestCasesInExecutables(vsTestCasesToRun.Select(tc => tc.Source).Distinct());

                IEnumerable<Model.TestCase> testCasesToRun = vsTestCasesToRun.Select(DataConversionExtensions.ToTestCase);
                DoRunTests(allTestCasesInExecutables, testCasesToRun, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            lock (this)
            {
                Canceled = true;
                Executor?.Cancel();
                TestEnvironment.LogInfo("Test execution canceled.");
            }
        }


        private void InitTestEnvironment(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            if (TestEnvironment == null || TestEnvironment.Options.GetType() == typeof(Options))
            {
                var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                RunSettings ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();

                ILogger loggerAdapter = new VsTestFrameworkLogger(messageLogger);
                TestEnvironment = new TestEnvironment(new Options(ourRunSettings, loggerAdapter), loggerAdapter);
            }
        }

        private IEnumerable<Model.TestCase> GetAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            List<Model.TestCase> allTestCasesInExecutables = new List<Model.TestCase>();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            foreach (string executable in executables.OrderBy(e => e))
            {
                if (Canceled)
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
            HashSet<string> allTraitNames = new HashSet<string>();
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
            IEnumerable<Model.TestCase> allTestCasesInExecutables, IEnumerable<Model.TestCase> testCasesToRun,
            IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            bool isRunningInsideVisualStudio = !string.IsNullOrEmpty(runContext.SolutionDirectory);
            var reporter = new VsTestFrameworkReporter(frameworkHandle, isRunningInsideVisualStudio);
            IDebuggedProcessLauncher launcher = new DebuggedProcessLauncher(frameworkHandle);
            Executor = new GoogleTestExecutor(TestEnvironment);
            Executor.RunTests(allTestCasesInExecutables, testCasesToRun, reporter, launcher,
                runContext.IsBeingDebugged, runContext.SolutionDirectory);
            reporter.AllTestsFinished();
        }

    }

}