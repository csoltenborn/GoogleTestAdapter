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

        private readonly TestEnvironment _testEnvironment;
        private readonly ITestRunner _innerTestRunner;
        private readonly int _threadId;
        private readonly string _solutionDirectory;


        public PreparingTestRunner(int threadId, string solutionDirectory, ITestFrameworkReporter reporter, TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
            _innerTestRunner = new SequentialTestRunner(reporter, _testEnvironment);
            _threadId = threadId;
            _solutionDirectory = solutionDirectory;
        }


        public void RunTests(IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, string baseDir,
             string workingDir, string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));
            DebugUtils.AssertIsNull(workingDir, nameof(workingDir));

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string testDirectory = Utils.GetTempDirectory();
                workingDir = _testEnvironment.Options.GetWorkingDir(_solutionDirectory, testDirectory, _threadId);
                userParameters = _testEnvironment.Options.GetUserParameters(_solutionDirectory, testDirectory, _threadId);

                string batch = _testEnvironment.Options.GetBatchForTestSetup(_solutionDirectory, testDirectory, _threadId);
                batch = batch == "" ? "" : _solutionDirectory + batch;
                SafeRunBatch(TestSetup, _solutionDirectory, batch, isBeingDebugged);

                _innerTestRunner.RunTests(allTestCases, testCasesToRun, baseDir, workingDir, userParameters, isBeingDebugged, debuggedLauncher, executor);

                batch = _testEnvironment.Options.GetBatchForTestTeardown(_solutionDirectory, testDirectory, _threadId);
                batch = batch == "" ? "" : _solutionDirectory + batch;
                SafeRunBatch(TestTeardown, _solutionDirectory, batch, isBeingDebugged);

                stopwatch.Stop();
                _testEnvironment.DebugInfo($"Thread {_threadId} took {stopwatch.Elapsed}");

                string errorMessage;
                if (!Utils.DeleteDirectory(testDirectory, out errorMessage))
                {
                    _testEnvironment.DebugWarning(
                        "Could not delete test directory '" + testDirectory + "': " + errorMessage);
                }
            }
            catch (Exception e)
            {
                _testEnvironment.LogError("Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            _innerTestRunner.Cancel();
        }


        private void SafeRunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            if (string.IsNullOrEmpty(batch))
            {
                return;
            }
            if (!File.Exists(batch))
            {
                _testEnvironment.LogError("Did not find " + batchType.ToLower() + " batch file: " + batch);
                return;
            }

            try
            {
                RunBatch(batchType, workingDirectory, batch, isBeingDebugged);
            }
            catch (Exception e)
            {
                _testEnvironment.LogError(
                    batchType + " batch caused exception, msg: '" + e.Message + "', executed command: '" +
                    batch + "'");
            }
        }

        private void RunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            int batchExitCode;
            new TestProcessLauncher(_testEnvironment, isBeingDebugged).GetOutputOfCommand(
                workingDirectory, batch, "", false, false, null, out batchExitCode);
            if (batchExitCode == 0)
            {
                _testEnvironment.DebugInfo(
                    $"Successfully ran {batchType} batch \'{batch}\'");
            }
            else
            {
                _testEnvironment.LogWarning(
                    $"{batchType} batch returned exit code {batchExitCode}, executed command: \'{batch}\'");
            }
        }

    }

}