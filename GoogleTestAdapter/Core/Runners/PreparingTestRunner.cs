using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Scheduling;

namespace GoogleTestAdapter.Runners
{
    public class PreparingTestRunner : ITestRunner
    {
        public const string TestSetup = "Test setup";
        public const string TestTeardown = "Test teardown";

        private readonly TestEnvironment _testEnvironment;
        private readonly ITestRunner _innerTestRunner;
        private readonly int _threadId;
        private readonly string _threadName;
        private readonly string _solutionDirectory;


        public PreparingTestRunner(int threadId, string solutionDirectory, ITestFrameworkReporter reporter, TestEnvironment testEnvironment, SchedulingAnalyzer schedulingAnalyzer)
        {
            _testEnvironment = testEnvironment;
            string threadName = ComputeThreadName(threadId, _testEnvironment.Options.MaxNrOfThreads);
            _threadName = string.IsNullOrEmpty(threadName) ? "" : $"{threadName} ";
            _threadId = Math.Max(0, threadId);
            _innerTestRunner = new SequentialTestRunner(_threadName, reporter, _testEnvironment, schedulingAnalyzer);
            _solutionDirectory = solutionDirectory;
        }

        public PreparingTestRunner(string solutionDirectory, ITestFrameworkReporter reporter,
            TestEnvironment testEnvironment, SchedulingAnalyzer schedulingAnalyzer)
            : this(-1, solutionDirectory, reporter, testEnvironment, schedulingAnalyzer){
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
                _testEnvironment.Logger.DebugInfo($"{_threadName}Execution took {stopwatch.Elapsed}");

                string errorMessage;
                if (!Utils.DeleteDirectory(testDirectory, out errorMessage))
                {
                    _testEnvironment.Logger.DebugWarning(
                        $"{_threadName}Could not delete test directory '" + testDirectory + "': " + errorMessage);
                }
            }
            catch (Exception e)
            {
                _testEnvironment.Logger.LogError($"{_threadName}Exception while running tests: " + e);
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
                _testEnvironment.Logger.LogError($"{_threadName}Did not find " + batchType.ToLower() + " batch file: " + batch);
                return;
            }

            try
            {
                RunBatch(batchType, workingDirectory, batch, isBeingDebugged);
            }
            catch (Exception e)
            {
                _testEnvironment.Logger.LogError(
                    $"{_threadName}{batchType} batch caused exception, msg: \'{e.Message}\', executed command: \'{batch}\'");
            }
        }

        private void RunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            int batchExitCode;
            if (_testEnvironment.Options.UseNewTestExecutionFramework)
            {
                var executor = new ProcessExecutor(null, _testEnvironment.Logger);
                batchExitCode = executor.ExecuteBatchFileBlocking(batch, "", workingDirectory, "", s => { });
            }
            else
            {
                new TestProcessLauncher(_testEnvironment, isBeingDebugged).GetOutputOfCommand(
                    workingDirectory, batch, "", false, false, null, out batchExitCode);
            }

            if (batchExitCode == 0)
            {
                _testEnvironment.Logger.DebugInfo(
                    $"{_threadName}Successfully ran {batchType} batch \'{batch}\'");
            }
            else
            {
                _testEnvironment.Logger.LogWarning(
                    $"{_threadName}{batchType} batch returned exit code {batchExitCode}, executed command: \'{batch}\'");
            }
        }

        private string ComputeThreadName(int threadId, int maxNrOfThreads)
        {
            if (threadId < 0)
                return "";

            int nrOfDigits = maxNrOfThreads.ToString().Length;
            string paddedThreadId = threadId.ToString().PadLeft(nrOfDigits, '0');

            return $"[T{paddedThreadId}]";
        }

    }

}