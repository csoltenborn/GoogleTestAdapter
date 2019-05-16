// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Helpers
{

    public class TestProcessLauncher
    {
        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly bool _isBeingDebugged;

        public TestProcessLauncher(ILogger logger, SettingsWrapper settings, bool isBeingDebugged)
        {
            _logger = logger;
            _settings = settings;
            _isBeingDebugged = isBeingDebugged;
        }

        public List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError, IDebuggedProcessLauncher debuggedLauncher)
        {
            return GetOutputOfCommand(workingDirectory, null, command, param, printTestOutput, throwIfError, debuggedLauncher);
        }

        public List<string> GetOutputOfCommand(string workingDirectory, IDictionary<string, string> envVars, string command, string param, bool printTestOutput,
            bool throwIfError, IDebuggedProcessLauncher debuggedLauncher)
        {
            int dummy;
            return GetOutputOfCommand(workingDirectory, envVars, command, param, printTestOutput, throwIfError, debuggedLauncher, out dummy);
        }

        public List<string> GetOutputOfCommand(string workingDirectory, IDictionary<string, string> envVars, string command, string param, bool printTestOutput,
            bool throwIfError, IDebuggedProcessLauncher debuggedLauncher, out int processExitCode)
        {
            if (_isBeingDebugged)
            {
                var output = new List<string>();
                processExitCode = LaunchProcessWithDebuggerAttached(workingDirectory, envVars, command, param, printTestOutput, debuggedLauncher);
                return output;
            }

            var actualLauncher = new ProcessLauncher(_logger, _settings.GetPathExtension(command), processId => _processId = processId);
            return actualLauncher.GetOutputOfCommand(workingDirectory, envVars, command, param, printTestOutput, 
                throwIfError, out processExitCode);
        }

        private int? _processId;

        public void Cancel()
        {
            if (_processId.HasValue)
                KillProcess(_processId.Value, _logger);
        }

        public static void KillProcess(int processId, ILogger logger)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                DateTime startTime = process.StartTime;
                try
                {
                    process.Kill();
                    logger.DebugInfo(String.Format(Resources.TerminatedProcess, process, startTime.ToShortTimeString()));
                }
                catch (Exception e)
                {
                    logger.DebugWarning(String.Format(Resources.CouldNotTerminate, process, startTime.ToShortTimeString(), e.Message));
                }
            }
            catch (Exception)
            {
                // process was not running - nothing to do
            }
        }


        private int LaunchProcessWithDebuggerAttached(string workingDirectory, IDictionary<string, string> envVars, string command, string param, bool printTestOutput,
            IDebuggedProcessLauncher handle)
        {
            _logger.LogInfo(String.Format(Resources.AttachDebuggerMessage, command));
            if (printTestOutput)
            {
                _logger.DebugInfo(Resources.DebuggerAttachedOutputMessage);
            }
            _processId = handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, envVars, param, _settings.GetPathExtension(command));
            Process process = Process.GetProcessById(_processId.Value);
            var waiter = new ProcessWaiter(process);
            waiter.WaitForExit();
            process.Dispose();
            return waiter.ProcessExitCode;
        }

    }

}