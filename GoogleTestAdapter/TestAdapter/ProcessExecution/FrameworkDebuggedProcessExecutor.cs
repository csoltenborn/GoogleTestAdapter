using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution.Contracts;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    public class FrameworkDebuggedProcessExecutor : IDebuggedProcessExecutor
    {
        private readonly ThreadSafeSingleShotGuard _guard = new ThreadSafeSingleShotGuard();

        private readonly IFrameworkHandle _frameworkHandle;
        private readonly bool _printTestOutput;
        private readonly ILogger _logger;
        
        private int _processId;

        public FrameworkDebuggedProcessExecutor(IFrameworkHandle handle, bool printTestOutput, ILogger logger)
        {
            _frameworkHandle = handle;
            _printTestOutput = printTestOutput;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension,
            Action<string> reportOutputLine)
        {
            if (reportOutputLine != null)
            {
                throw new ArgumentException(nameof(reportOutputLine));
            }
            if (_guard.CheckAndSetFirstCall)
            {
                throw new InvalidOperationException();
            }

            _logger.DebugInfo($"Attaching debugger to '{command}'");
            if (_printTestOutput)
            {
                _logger.DebugInfo(
                    "Note that due to restrictions of the VS Unit Testing framework, the test executable's output can not be displayed in the test console when debugging tests!");
            }

            IDictionary<string, string> envVariables = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(pathExtension))
                envVariables["PATH"] = Utils.GetExtendedPath(pathExtension);

            _processId = _frameworkHandle.LaunchProcessWithDebuggerAttached(command, workingDir, parameters, envVariables);

            var process = Process.GetProcessById(_processId);
            var waiter = new ProcessWaiter(process);
            waiter.WaitForExit();
            process.Dispose();

            return waiter.ProcessExitCode;
        }

        public void Cancel()
        {
            ProcessUtils.KillProcess(_processId, _logger);
        }

    }

}