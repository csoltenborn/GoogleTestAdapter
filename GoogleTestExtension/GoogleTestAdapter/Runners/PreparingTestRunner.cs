using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Runners
{
    class PreparingTestRunner : ITestRunner
    {
        private TestEnvironment TestEnvironment { get; }
        private ITestRunner InnerTestRunner { get; }
        private int ThreadId { get; }

        internal PreparingTestRunner(int threadId, TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
            this.InnerTestRunner = new SequentialTestRunner(TestEnvironment);
            this.ThreadId = threadId;
        }

        void ITestRunner.RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            try
            {
                string testDirectory = Utils.GetTempDirectory();
                userParameters = TestEnvironment.Options.GetUserParameters(runContext.SolutionDirectory, testDirectory, ThreadId);

                string batch = TestEnvironment.Options.GetTestSetupBatch(runContext.SolutionDirectory, testDirectory, ThreadId);
                SafeRunBatch("Test setup", batch, runContext, handle);

                InnerTestRunner.RunTests(allTestCases, testCasesToRun, userParameters, runContext, handle);

                batch = TestEnvironment.Options.GetTestTeardownBatch(runContext.SolutionDirectory, testDirectory, ThreadId);
                SafeRunBatch("Test teardown", batch, runContext, handle);

                Directory.Delete(testDirectory);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        void ITestRunner.Cancel()
        {
            InnerTestRunner.Cancel();
        }

        private void SafeRunBatch(string batchType, string batch, IRunContext runContext, IFrameworkHandle handle)
        {
            if (string.IsNullOrEmpty(batch))
            {
                return;
            }

            try
            {
                RunBatch(batchType, batch, runContext, handle);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError(
                    batchType + " batch caused exception, msg: '" + e.Message + "', executed command: '" +
                    batch + "'");
            }
        }

        private void RunBatch(string batchType, string batch, IRunContext runContext, IFrameworkHandle handle)
        {
            int batchExitCode;
            new ProcessLauncher(TestEnvironment).GetOutputOfCommand("", batch, "", false, false, runContext, null,
                out batchExitCode);
            if (batchExitCode == 0)
            {
                TestEnvironment.LogInfo(
                    "Successfully ran " + batchType + "batch '" + batch + "'", TestEnvironment.LogType.UserDebug);
            }
            else
            {
                TestEnvironment.LogWarning(
                    batchType + " batch returned exit code " + batchExitCode + ", executed command: '" +
                    batch + "'");
            }
        }

    }

}