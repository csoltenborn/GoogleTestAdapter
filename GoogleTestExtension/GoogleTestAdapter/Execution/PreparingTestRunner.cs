using System;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Execution
{
    public class PreparingTestRunner : AbstractGoogleTestAdapterClass, IGoogleTestRunner
    {
        private IGoogleTestRunner InnerTestRunner { get; }
        private int ThreadId { get; }

        public bool Canceled { get; set; } = false;

        public PreparingTestRunner(IGoogleTestRunner innerTestrunner, AbstractOptions options, int threadId) : base(options) {
            this.InnerTestRunner = innerTestrunner;
            this.ThreadId = threadId;
        }

        public void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle, string testDirectory)
        {
            try
            {
                if (testDirectory != null)
                {
                    throw new ArgumentException("testDirectory must be null");
                }

                testDirectory = Utils.GetTempDirectory();
                string userParameters = Options.GetUserParameters(runContext.SolutionDirectory, testDirectory, ThreadId);

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

    }

}