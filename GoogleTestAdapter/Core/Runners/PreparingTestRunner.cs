using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Runners
{
    public class PreparingTestRunner : ITestRunner
    {
        public const string TestSetup = "Test setup";
        public const string TestTeardown = "Test teardown";

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly ITestRunner _innerTestRunner;
        private readonly int _threadId;
        private readonly string _threadName;
        private readonly string _solutionDirectory;


        public PreparingTestRunner(int threadId, string solutionDirectory, ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
        {
            _logger = logger;
            _settings = settings;
            string threadName = ComputeThreadName(threadId, _settings.MaxNrOfThreads);
            _threadName = string.IsNullOrEmpty(threadName) ? "" : $"{threadName} ";
            _threadId = Math.Max(0, threadId);
            _innerTestRunner = new SequentialTestRunner(_threadName, reporter, _logger, _settings, schedulingAnalyzer);
            _solutionDirectory = solutionDirectory;
        }

        public PreparingTestRunner(string solutionDirectory, ITestFrameworkReporter reporter,
            ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
            : this(-1, solutionDirectory, reporter, logger, settings, schedulingAnalyzer){
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, string baseDir,
             string workingDir, string userParameters, bool isBeingDebugged, IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            DebugUtils.AssertIsNull(userParameters, nameof(userParameters));
            DebugUtils.AssertIsNull(workingDir, nameof(workingDir));

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string testDirectory = Utils.GetTempDirectory();
                workingDir = _settings.GetWorkingDir(_solutionDirectory, testDirectory, _threadId);
                userParameters = _settings.GetUserParameters(_solutionDirectory, testDirectory, _threadId);

                string batch = _settings.GetBatchForTestSetup(_solutionDirectory, testDirectory, _threadId);
                batch = batch == "" ? "" : _solutionDirectory + batch;
                SafeRunBatch(TestSetup, _solutionDirectory, batch, isBeingDebugged);

                _innerTestRunner.RunTests(testCasesToRun, baseDir, workingDir, userParameters, isBeingDebugged, debuggedLauncher, executor);

                batch = _settings.GetBatchForTestTeardown(_solutionDirectory, testDirectory, _threadId);
                batch = batch == "" ? "" : _solutionDirectory + batch;
                SafeRunBatch(TestTeardown, _solutionDirectory, batch, isBeingDebugged);

                stopwatch.Stop();
                _logger.DebugInfo($"{_threadName}Execution took {stopwatch.Elapsed}");

                string errorMessage;
                if (!Utils.DeleteDirectory(testDirectory, out errorMessage))
                {
                    _logger.DebugWarning(
                        $"{_threadName}Could not delete test directory '" + testDirectory + "': " + errorMessage);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"{_threadName}Exception while running tests: " + e);
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
                _logger.LogError($"{_threadName}Did not find " + batchType.ToLower() + " batch file: " + batch);
                return;
            }

            try
            {
                RunBatch(batchType, workingDirectory, batch, isBeingDebugged);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"{_threadName}{batchType} batch caused exception, msg: \'{e.Message}\', executed command: \'{batch}\'");
            }
        }

        private void RunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            int batchExitCode;
            if (_settings.UseNewTestExecutionFramework)
            {
                var executor = new ProcessExecutor(null, _logger);
                batchExitCode = executor.ExecuteBatchFileBlocking(batch, "", workingDirectory, "", s => { });
            }
            else
            {
                new TestProcessLauncher(_logger, _settings, isBeingDebugged).GetOutputOfCommand(
                    workingDirectory, batch, "", false, false, null, out batchExitCode);
            }

            if (batchExitCode == 0)
            {
                _logger.DebugInfo(
                    $"{_threadName}Successfully ran {batchType} batch \'{batch}\'");
            }
            else
            {
                _logger.LogWarning(
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