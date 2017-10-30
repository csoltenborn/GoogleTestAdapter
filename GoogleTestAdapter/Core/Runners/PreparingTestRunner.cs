// This file has been modified by Microsoft on 8/2017.

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
        private enum BatchType
        {
            TestSetup,
            TestTeardown
        }

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
                SafeRunBatch(BatchType.TestSetup, _solutionDirectory, batch, isBeingDebugged);

                _innerTestRunner.RunTests(testCasesToRun, baseDir, workingDir, userParameters, isBeingDebugged, debuggedLauncher, executor);

                batch = _settings.GetBatchForTestTeardown(_solutionDirectory, testDirectory, _threadId);
                batch = batch == "" ? "" : _solutionDirectory + batch;
                SafeRunBatch(BatchType.TestTeardown, _solutionDirectory, batch, isBeingDebugged);

                stopwatch.Stop();
                _logger.DebugInfo(String.Format(Resources.ExecutionTime, _threadName, stopwatch.Elapsed));

                string errorMessage;
                if (!Utils.DeleteDirectory(testDirectory, out errorMessage))
                {
                    _logger.DebugWarning(String.Format(Resources.DeleteTestDir, _threadName, testDirectory, errorMessage));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(String.Format(Resources.ExceptionMessage, _threadName, e));
            }
        }

        public void Cancel()
        {
            _innerTestRunner.Cancel();
        }


        private void SafeRunBatch(BatchType batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            string batchTypeString = (batchType == BatchType.TestSetup) ? Resources.TestSetupBatchFile : Resources.TestTeardownBatchFile;

            if (string.IsNullOrEmpty(batch))
            {
                return;
            }
            if (!File.Exists(batch))
            {
                _logger.LogError(String.Format(Resources.BatchFileMissing, _threadName, batchTypeString, batch));
                return;
            }

            try
            {
                RunBatch(batchType, workingDirectory, batch, isBeingDebugged);
            }
            catch (Exception e)
            {
                _logger.LogError(String.Format(Resources.RunBatchException, _threadName, batchTypeString, e.Message, batch));
            }
        }

        private void RunBatch(BatchType batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            string batchTypeString = (batchType == BatchType.TestSetup) ? Resources.TestSetupBatchFile : Resources.TestTeardownBatchFile;

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
                _logger.DebugInfo(String.Format(Resources.SuccessfullyRun, _threadName, batchTypeString, batch));
            }
            else
            {
                _logger.LogWarning(String.Format(Resources.BatchReturnedExitCode, _threadName, batchTypeString, batchExitCode, batch));
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