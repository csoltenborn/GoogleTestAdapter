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

        internal PreparingTestRunner(IGoogleTestRunner innerTestrunner, AbstractOptions options, int threadId) : base(options) {
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

                // ProcessUtils.GetOutputOfCommand(handle, "", "", "", false, false, runContext, handle);
                InnerTestRunner.RunTests(runAllTestCases, allTestCases, testCasesToRun, runContext, handle, userParameters);
                // ProcessUtils.GetOutputOfCommand(handle, "", "", "", false, false, runContext, handle);

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

    }

}