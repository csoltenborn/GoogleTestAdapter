using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter
{
    class DebuggedProcessLauncher : IDebuggedProcessLauncher
    {
        private IFrameworkHandle Handle { get; }

        internal DebuggedProcessLauncher(IFrameworkHandle handle)
        {
            Handle = handle;
        }

        public int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, string param)
        {
            return Handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, null);
        }
    }

    [ExtensionUri(ExecutorUriString)]
    public class GoogleTestExecutor : ITestExecutor
    {
        internal const string ExecutorUriString = "executor://GoogleTestRunner/v1";
        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);


        private TestEnvironment TestEnvironment { get; set; }

        private List<TestCase2> AllTestCasesInExecutables { get; } = new List<TestCase2>();
        private ITestRunner Runner { get; set; }
        private bool Canceled { get; set; } = false;


        public GoogleTestExecutor() : this(null) { }

        public GoogleTestExecutor(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
        }


        public void RunTests(IEnumerable<string> executables, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            try
            {
                InitTestEnvironment(runContext.RunSettings, frameworkHandle);

                ComputeAllTestCasesInExecutables(executables);

                DoRunTests(AllTestCasesInExecutables, runContext, frameworkHandle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void RunTests(IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            IEnumerable<TestCase2> ourTestCasesToRun = testCasesToRun.Select(Extensions.ToTestCase);
            try
            {
                InitTestEnvironment(runContext.RunSettings, frameworkHandle);

                TestCase2[] testCasesToRunAsArray = ourTestCasesToRun as TestCase2[] ?? ourTestCasesToRun.ToArray();
                ComputeAllTestCasesInExecutables(testCasesToRunAsArray.Select(tc => tc.Source).Distinct());

                DoRunTests(testCasesToRunAsArray, runContext, frameworkHandle);
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
                Runner?.Cancel();
                TestEnvironment.LogInfo("Test execution canceled.");
            }
        }


        private void InitTestEnvironment(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            if (TestEnvironment == null || TestEnvironment.Options.GetType() == typeof(Options))
            {
                var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                RunSettings ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();

                TestEnvironment = new TestEnvironment(new Options(ourRunSettings, messageLogger), messageLogger);
            }

            TestEnvironment.CheckDebugModeForExecutionCode();
        }

        private void DoRunTests(IEnumerable<TestCase2> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            TestCase2[] testCasesToRunAsArray = testCasesToRun as TestCase2[] ?? testCasesToRun.ToArray();
            TestEnvironment.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (Canceled)
                {
                    return;
                }
                ComputeTestRunner(runContext, handle);
            }

            Runner.RunTests(AllTestCasesInExecutables, testCasesToRunAsArray, null, runContext.IsBeingDebugged, new DebuggedProcessLauncher(handle));
            TestEnvironment.LogInfo("Test execution completed.");
        }

        private void ComputeTestRunner(IRunContext runContext, IFrameworkHandle handle)
        {
            if (TestEnvironment.Options.ParallelTestExecution && !runContext.IsBeingDebugged)
            {
                Runner = new ParallelTestRunner(new VsTestFrameworkReporter(null, handle, TestEnvironment), TestEnvironment, runContext.SolutionDirectory);
            }
            else
            {
                Runner = new PreparingTestRunner(0, runContext.SolutionDirectory, new VsTestFrameworkReporter(null, handle, TestEnvironment), TestEnvironment);
                if (TestEnvironment.Options.ParallelTestExecution && runContext.IsBeingDebugged)
                {
                    TestEnvironment.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

        private void ComputeAllTestCasesInExecutables(IEnumerable<string> executables)
        {
            AllTestCasesInExecutables.Clear();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
            foreach (string executable in executables)
            {
                if (Canceled)
                {
                    AllTestCasesInExecutables.Clear();
                    break;
                }

                AllTestCasesInExecutables.AddRange(discoverer.GetTestsFromExecutable(executable));
            }
        }

    }

    public static class Conversions
    {
        public static TestCase ToVsTestCase(this TestCase2 testCase)
        {
            TestCase result = new TestCase(testCase.FullyQualifiedName, testCase.ExecutorUri, testCase.Source);
            result.DisplayName = testCase.DisplayName;
            return result;
        }
    }

}