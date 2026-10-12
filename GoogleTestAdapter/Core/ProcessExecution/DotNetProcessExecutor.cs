using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.ProcessExecution
{

    public class DotNetProcessExecutor : IProcessExecutor
    {
        private readonly bool _printTestOutput;
        private readonly ILogger _logger;
        
        private Process _process;
        private readonly object _lock = new object();
        // standard output and standard error are read by different threads
        private readonly object _outputLock = new object();
        private bool _canceled;
        private int? _processId;
        private JobObject _job;

        public static void LogStartOfOutput(ILogger logger, string command, string parameters)
        {
            logger.LogInfo(
                ">>>>>>>>>>>>>>> Output of command '" + command + " " + parameters + "'");
        }

        public static void LogEndOfOutput(ILogger logger)
        {
            logger.LogInfo("<<<<<<<<<<<<<<< End of Output");
        }

        public DotNetProcessExecutor(bool printTestOutput, ILogger logger)
        {
            _printTestOutput = printTestOutput;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, IDictionary<string, string> environmentVariables,
            Action<string> reportOutputLine)
        {
            // output reading after https://stackoverflow.com/a/7608823/1276129
            var processStartInfo = new ProcessStartInfo(command, parameters)
            {
                StandardOutputEncoding = ProcessOutputEncoding.Raw,
                StandardErrorEncoding = ProcessOutputEncoding.Raw,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            if (!string.IsNullOrEmpty(pathExtension))
                processStartInfo.EnvironmentVariables["PATH"] = Utils.GetExtendedPath(pathExtension);

            foreach (var environmentVariable in environmentVariables)
                processStartInfo.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;

            _process = new Process {StartInfo = processStartInfo};
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            using (_process)
            {
                void HandleEvent(string line, AutoResetEvent autoResetEvent)
                {
                    if (line == null)
                    {
                        autoResetEvent.Set();
                    }
                    else
                    {
                        line = ProcessOutputEncoding.DecodeLine(line);
                        lock (_outputLock)
                        {
                            reportOutputLine(line);
                            if (_printTestOutput)
                            {
                                _logger.LogInfo(line);
                            }
                        }
                    }
                }

                // ReSharper disable AccessToDisposedClosure
                _process.OutputDataReceived += (sender, e) => HandleEvent(e.Data, outputWaitHandle);
                _process.ErrorDataReceived += (sender, e) => HandleEvent(e.Data, errorWaitHandle);
                // ReSharper restore AccessToDisposedClosure

                if (_printTestOutput)
                {
                    LogStartOfOutput(_logger, command, parameters);
                }

                JobObject job;
                lock (_lock)
                {
                    // the executable is not started at all if execution has been canceled before
                    if (_canceled)
                    {
                        _logger.DebugInfo($"Executable {command} has not been started since execution has been canceled");
                        return int.MaxValue;
                    }

                    _process.Start();
                    _processId = _process.Id;
                    _job = job = JobObject.TryCreate(_process.Handle, _process.Id, _logger);
                }

                using (job)
                {
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();

                    if (_process.WaitForExit(int.MaxValue) &&
                        outputWaitHandle.WaitOne(int.MaxValue) &&
                        errorWaitHandle.WaitOne(int.MaxValue))
                    {
                        if (_printTestOutput)
                        {
                            LogEndOfOutput(_logger);
                        }

                        _logger.DebugInfo($"Executable {command} returned with exit code {_process.ExitCode}");
                        return _process.ExitCode;
                    }

                    _logger.DebugInfo($"Executable {command} did not return properly, using return code {int.MaxValue}");
                    return int.MaxValue;
                }
            }
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _canceled = true;
                if (_job != null)
                {
                    _job.Terminate();
                }
                else if (_processId.HasValue)
                {
                    ProcessUtils.KillProcess(_processId.Value, _logger);
                }
            }
        }

    }

}