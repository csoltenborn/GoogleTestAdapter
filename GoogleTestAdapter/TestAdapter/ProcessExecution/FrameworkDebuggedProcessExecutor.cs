using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    public class FrameworkDebuggedProcessExecutor : IDebuggedProcessExecutor
    {
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly bool _printTestOutput;
        private readonly ILogger _logger;
        
        private int? _processId;

        public FrameworkDebuggedProcessExecutor(IFrameworkHandle handle, bool printTestOutput, ILogger logger)
        {
            _frameworkHandle = handle;
            _printTestOutput = printTestOutput;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, IDictionary<string, string> environmentVariables,
            Action<string> reportOutputLine)
        {
            if (reportOutputLine != null)
            {
                throw new ArgumentException(nameof(reportOutputLine));
            }
            if (_processId.HasValue)
            {
                throw new InvalidOperationException();
            }

            if (!string.IsNullOrEmpty(pathExtension))
                environmentVariables["PATH"] = Utils.GetExtendedPath(pathExtension);

            _logger.DebugInfo($"Attaching debugger to '{command}' via {DebuggerKind.VsTestFramework} engine");
            if (_printTestOutput)
            {
                _logger.DebugInfo(
                    $"Note that due to restrictions of the VsTest framework, the test executable's output can not be displayed in the test console when debugging tests. Use '{SettingsWrapper.OptionDebuggerKind}' option to overcome this problem.'");
            }

            _processId = _frameworkHandle.LaunchProcessWithDebuggerAttached(command, workingDir, parameters, environmentVariables);

            ProcessWaiter waiter;
            using (var process = Process.GetProcessById(_processId.Value))
            {
                waiter = new ProcessWaiter(process);
                waiter.WaitForExit();
            }

            _logger.DebugInfo($"Executable {command} returned with exit code {waiter.ProcessExitCode}");
            return waiter.ProcessExitCode;
        }

        public void Cancel()
        {
            if (_processId.HasValue)
            {
                ProcessUtils.KillProcess(_processId.Value, _logger);
            }
        }

    }

}