using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.VS.Helpers;
using GoogleTestAdapter.VS.Framework;
using GoogleTestAdapter.VS.Settings;

namespace GoogleTestAdapter.VS
{

    [ExtensionUri(GoogleTestExecutor.ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
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
                IEnumerable<GoogleTestAdapter.Model.TestCase> allTestCasesInExecutables =
                    GetAllTestCasesInExecutables(executables);

                DoRunTests(allTestCasesInExecutables, allTestCasesInExecutables, runContext, frameworkHandle);
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
                IEnumerable<GoogleTestAdapter.Model.TestCase> allTestCasesInExecutables =
                    GetAllTestCasesInExecutables(vsTestCasesToRun.Select(tc => tc.Source).Distinct());

                IEnumerable<GoogleTestAdapter.Model.TestCase> testCasesToRun = vsTestCasesToRun.Select(DataConversionExtensions.ToTestCase);
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

            new DebugHelper(TestEnvironment).CheckDebugModeForExecutionCode();
        }

        private IEnumerable<GoogleTestAdapter.Model.TestCase> GetAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            List<GoogleTestAdapter.Model.TestCase> allTestCasesInExecutables = new List<GoogleTestAdapter.Model.TestCase>();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            foreach (string executable in executables)
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

        private void DoRunTests(IEnumerable<GoogleTestAdapter.Model.TestCase> allTestCasesInExecutables,
            IEnumerable<GoogleTestAdapter.Model.TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            ITestFrameworkReporter reporter = new VsTestFrameworkReporter(null, frameworkHandle, TestEnvironment);
            IDebuggedProcessLauncher launcher = new DebuggedProcessLauncher(frameworkHandle);
            Executor = new GoogleTestExecutor(TestEnvironment);
            Executor.RunTests(allTestCasesInExecutables, testCasesToRun, reporter, launcher,
                runContext.IsBeingDebugged, runContext.SolutionDirectory);
        }

    }

}