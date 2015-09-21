using System;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Runners
{
    class PreparingTestRunner : AbstractOptionsProvider, IGoogleTestRunner
    {
        private IGoogleTestRunner InnerTestRunner { get; }
        private int ThreadId { get; }

        internal PreparingTestRunner(int threadId, AbstractOptions options) : base(options)
        {
            this.InnerTestRunner = new SequentialTestRunner(options);
            this.ThreadId = threadId;
        }

        void IGoogleTestRunner.RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
            string userParameters, IRunContext runContext, IFrameworkHandle handle)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            try
            {
                string testDirectory = Utils.GetTempDirectory();
                userParameters = Options.GetUserParameters(runContext.SolutionDirectory, testDirectory, ThreadId);

                string batch = Options.GetTestSetupBatch(runContext.SolutionDirectory, testDirectory, ThreadId);
                SafeRunBatch("Test setup", batch, runContext, handle);

                InnerTestRunner.RunTests(runAllTestCases, allTestCases, testCasesToRun, userParameters, runContext, handle);

                batch = Options.GetTestTeardownBatch(runContext.SolutionDirectory, testDirectory, ThreadId);
                SafeRunBatch("Test teardown", batch, runContext, handle);

                Directory.Delete(testDirectory);
            }
            catch (Exception e)
            {
                handle.SendMessage(TestMessageLevel.Error, "GTA: Exception while running tests: " + e);
            }
        }

        void IGoogleTestRunner.Cancel()
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
                handle.SendMessage(TestMessageLevel.Error,
                    "GTA: " + batchType + " batch caused exception, msg: '" + e.Message + "', executed command: '" +
                    batch + "'");
            }
        }

        private void RunBatch(string batchType, string batch, IRunContext runContext, IFrameworkHandle handle)
        {
            int batchExitCode;
            new ProcessLauncher(Options).GetOutputOfCommand(handle, "", batch, "", false, false, runContext, null,
                out batchExitCode);
            if (batchExitCode == 0)
            {
                DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational,
                    "Successfully ran " + batchType + "batch '" + batch + "'");
            }
            else
            {
                handle.SendMessage(TestMessageLevel.Warning,
                    "GTA: " + batchType + " batch returned exit code " + batchExitCode + ", executed command: '" +
                    batch + "'");
            }
        }

    }

}