﻿// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Helpers
{

    public class ProcessLauncher
    {
        private readonly ILogger _logger;
        private readonly string _pathExtension;
        private readonly Action<int> _reportProcessId;
        
        private Process _process;

        public ProcessLauncher(ILogger logger) : this(logger, "", null)
        {
        }

        public ProcessLauncher(ILogger logger, string pathExtension, Action<int> reportProcessId)
        {
            _logger = logger;
            _pathExtension = pathExtension;
            _reportProcessId = reportProcessId;
        }

        public List<string> GetOutputOfCommand(string command)
        {
            int dummy;
            return GetOutputOfCommand("", null, command, "", false, false, out dummy);
        }

        public List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError)
        {
            int dummy;
            return GetOutputOfCommand(workingDirectory, null, command, param, printTestOutput, throwIfError, out dummy);
        }

        public List<string> GetOutputOfCommand(string workingDirectory, IDictionary<string, string> envVars, string command, string param, bool printTestOutput,
            bool throwIfError, out int processExitCode)
        {
            var output = new List<string>();
            processExitCode = LaunchProcess(workingDirectory, envVars, command, param, printTestOutput, throwIfError, output);
            return output;
        }

        public void Cancel()
        {
            if (_process != null)
            {
                TestProcessLauncher.KillProcess(_process.Id, _logger);
            }
        }


        private int LaunchProcess(string workingDirectory, IDictionary<string, string> additionalEnvVars, string command, string param, bool printTestOutput,
            bool throwIfError, List<string> output)
        {
            var processStartInfo = new ProcessStartInfo(command, param)
            {
                StandardOutputEncoding = Encoding.Default,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            if (additionalEnvVars != null)
            {
                foreach (var entry in additionalEnvVars)
                {
                    processStartInfo.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }

            if (!string.IsNullOrEmpty(_pathExtension))
            {
                var path = Utils.GetExtendedPath(_pathExtension);
                if (processStartInfo.EnvironmentVariables.ContainsKey("PATH"))
                {
                    path += $";{processStartInfo.EnvironmentVariables["PATH"]}";
                }
                processStartInfo.EnvironmentVariables["PATH"] = path;
            }

            _process = Process.Start(processStartInfo);
            if (_process != null)
                _reportProcessId?.Invoke(_process.Id);
            try
            {
                var waiter = new ProcessWaiter(_process);
                if (printTestOutput)
                {
                    _logger.LogInfo(String.Format(Resources.OutputOfCommandMessage, "", command, param));
                }
                ReadTheStream(_process, output, printTestOutput, throwIfError);
                if (printTestOutput)
                {
                    _logger.LogInfo(String.Format(Resources.EndOfOutputMessage, ""));
                }
                return waiter.WaitForExit();
            }
            finally
            {
                _process?.Dispose();
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void ReadTheStream(Process process, List<string> streamContent, bool printTestOutput, bool throwIfError)
        {
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                streamContent.Add(line);
                if (printTestOutput)
                {
                    _logger.LogInfo(line);
                }
            }
            if (throwIfError && process.ExitCode != 0)
            {
                throw new Exception(String.Format(Resources.ProcessExitCode, process.ExitCode));
            }
        }

    }

}