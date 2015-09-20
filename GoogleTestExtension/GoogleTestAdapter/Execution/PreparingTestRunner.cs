using System;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Execution
{
    class PreparingTestRunner : AbstractGoogleTestAdapterClass, IGoogleTestRunner
    {
        private IGoogleTestRunner InnerTestRunner { get; }
        private int ThreadId { get; }

        internal PreparingTestRunner(SequentialTestRunner innerTestrunner, AbstractOptions options, int threadId) : base(options) {
            this.InnerTestRunner = innerTestrunner;
            this.ThreadId = threadId;
        }

        void IGoogleTestRunner.RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle, string userParameters)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            try
            {
                string testDirectory = Utils.GetTempDirectory();
                userParameters = Options.GetUserParameters(runContext.SolutionDirectory, testDirectory, ThreadId);

                string batch = Options.GetTestSetupBatch(runContext.SolutionDirectory, testDirectory, ThreadId);
                ExecuteBatch(runContext, handle, batch, "Test setup");

                InnerTestRunner.RunTests(runAllTestCases, allTestCases, testCasesToRun, runContext, handle, userParameters);

                batch = Options.GetTestTeardownBatch(runContext.SolutionDirectory, testDirectory, ThreadId);
                ExecuteBatch(runContext, handle, batch, "Test teardown");

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

        private void ExecuteBatch(IRunContext runContext, IFrameworkHandle handle, string batch, string batchType)
        {
            if (string.IsNullOrEmpty(batch))
            {
                return;
            }

            try
            {
                int batchExitCode;
                ProcessUtils.GetOutputOfCommand(handle, "", batch, "", false, false, runContext, null,
                    out batchExitCode);
                if (batchExitCode == 0)
                {
                    DebugUtils.LogUserDebugMessage(handle, Options, TestMessageLevel.Informational, "Successfully ran " + batchType + "batch '" + batch + "'");
                }
                else
                {
                    handle.SendMessage(TestMessageLevel.Warning,
                        "GTA: " + batchType + " batch returned exit code " + batchExitCode + ", executed command: '" +
                        batch + "'");
                }
            }
            catch (Exception e)
            {
                handle.SendMessage(TestMessageLevel.Error,
                    "GTA: " + batchType + " batch caused exception, msg: '" + e.Message + "', executed command: '" +
                    batch + "'");
            }
        }

    }

}