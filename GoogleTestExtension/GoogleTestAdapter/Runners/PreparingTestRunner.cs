using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Runners
{
    public class PreparingTestRunner : ITestRunner
    {
        public const string TEST_SETUP = "Test setup";
        public const string TEST_TEARDOWN = "Test teardown";

        private TestEnvironment TestEnvironment { get; }
        private ITestRunner InnerTestRunner { get; }
        private int ThreadId { get; }


        public PreparingTestRunner(int threadId, TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
            this.InnerTestRunner = new SequentialTestRunner(TestEnvironment);
            this.ThreadId = threadId;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            try
            {
                string testDirectory = Utils.GetTempDirectory();
                userParameters = TestEnvironment.Options.GetUserParameters(runContext.SolutionDirectory, testDirectory, ThreadId);

                string batch = TestEnvironment.Options.GetBatchForTestSetup(runContext.SolutionDirectory, testDirectory, ThreadId);
                batch = batch == "" ? "" : runContext.SolutionDirectory + batch;
                SafeRunBatch(TEST_SETUP, runContext.SolutionDirectory, batch, runContext);

                InnerTestRunner.RunTests(allTestCases, testCasesToRun, userParameters, runContext, handle);

                batch = TestEnvironment.Options.GetBatchForTestTeardown(runContext.SolutionDirectory, testDirectory, ThreadId);
                batch = batch == "" ? "" : runContext.SolutionDirectory + batch;
                SafeRunBatch(TEST_TEARDOWN, runContext.SolutionDirectory, batch, runContext);

                string errorMessage;
                if (!Utils.DeleteDirectory(testDirectory, out errorMessage))
                {
                    TestEnvironment.DebugWarning(
                        "Could not delete test directory '" + testDirectory + "': " + errorMessage);
                }
            }
            catch (Exception e)
            {
                TestEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            InnerTestRunner.Cancel();
        }


        private void SafeRunBatch(string batchType, string workingDirectory, string batch, IRunContext runContext)
        {
            if (string.IsNullOrEmpty(batch))
            {
                return;
            }
            if (!File.Exists(batch))
            {
                TestEnvironment.LogError("Did not find " + batchType.ToLower() + " batch file: " + batch);
                return;
            }

            try
            {
                RunBatch(batchType, workingDirectory, batch, runContext);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError(
                    batchType + " batch caused exception, msg: '" + e.Message + "', executed command: '" +
                    batch + "'");
            }
        }

        private void RunBatch(string batchType, string workingDirectory, string batch, IRunContext runContext)
        {
            int batchExitCode;
            new ProcessLauncher(TestEnvironment).GetOutputOfCommand(
                workingDirectory, batch, "", false, false, runContext, null, out batchExitCode);
            if (batchExitCode == 0)
            {
                TestEnvironment.DebugInfo(
                    "Successfully ran " + batchType + "batch '" + batch + "'");
            }
            else
            {
                TestEnvironment.LogWarning(
                    batchType + " batch returned exit code " + batchExitCode
                    + ", executed command: '" + batch + "'");
            }
        }

    }

}