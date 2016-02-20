using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Runners
{
    public class PreparingTestRunner : ITestRunner
    {
        public const string TestSetup = "Test setup";
        public const string TestTeardown = "Test teardown";

        private TestEnvironment TestEnvironment { get; }
        private ITestRunner InnerTestRunner { get; }
        private int ThreadId { get; }
        private string SolutionDirectory { get; }


        public PreparingTestRunner(int threadId, string solutionDirectory, ITestFrameworkReporter reporter, TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
            this.InnerTestRunner = new SequentialTestRunner(reporter, TestEnvironment);
            this.ThreadId = threadId;
            this.SolutionDirectory = solutionDirectory;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
            string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string testDirectory = Utils.GetTempDirectory();
                userParameters = TestEnvironment.Options.GetUserParameters(SolutionDirectory, testDirectory, ThreadId);

                string batch = TestEnvironment.Options.GetBatchForTestSetup(SolutionDirectory, testDirectory, ThreadId);
                batch = batch == "" ? "" : SolutionDirectory + batch;
                SafeRunBatch(TestSetup, SolutionDirectory, batch, isBeingDebugged);

                InnerTestRunner.RunTests(allTestCases, testCasesToRun, baseDir, userParameters, isBeingDebugged, debuggedLauncher);

                batch = TestEnvironment.Options.GetBatchForTestTeardown(SolutionDirectory, testDirectory, ThreadId);
                batch = batch == "" ? "" : SolutionDirectory + batch;
                SafeRunBatch(TestTeardown, SolutionDirectory, batch, isBeingDebugged);

                stopwatch.Stop();
                TestEnvironment.DebugInfo($"Thread {ThreadId} took {stopwatch.Elapsed}");

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


        private void SafeRunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
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
                RunBatch(batchType, workingDirectory, batch, isBeingDebugged);
            }
            catch (Exception e)
            {
                TestEnvironment.LogError(
                    batchType + " batch caused exception, msg: '" + e.Message + "', executed command: '" +
                    batch + "'");
            }
        }

        private void RunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            int batchExitCode;
            new TestProcessLauncher(TestEnvironment, isBeingDebugged).GetOutputOfCommand(
                workingDirectory, batch, "", false, false, null, out batchExitCode);
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