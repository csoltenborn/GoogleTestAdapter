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

        public ProcessLauncher(ILogger logger, string pathExtension)
        {
            _logger = logger;
            _pathExtension = pathExtension;
        }

        public List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError)
        {
            int dummy;
            return GetOutputOfCommand(workingDirectory, command, param, printTestOutput, throwIfError, out dummy);
        }

        public List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError, out int processExitCode)
        {
            var output = new List<string>();
            processExitCode = LaunchProcess(workingDirectory, command, param, printTestOutput, throwIfError, output);
            return output;
        }


        private int LaunchProcess(string workingDirectory, string command, string param, bool printTestOutput,
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

            if (!string.IsNullOrEmpty(_pathExtension))
                processStartInfo.EnvironmentVariables["PATH"] = Utils.GetExtendedPath(_pathExtension);

            Process process = Process.Start(processStartInfo);
            try
            {
                var waiter = new ProcessWaiter(process);
                if (printTestOutput)
                {
                    _logger.LogInfo(
                        ">>>>>>>>>>>>>>> Output of command '" + command + " " + param + "'");
                }
                ReadTheStream(process, output, printTestOutput, throwIfError);
                if (printTestOutput)
                {
                    _logger.LogInfo("<<<<<<<<<<<<<<< End of Output");
                }
                return waiter.WaitForExit();
            }
            finally
            {
                process?.Dispose();
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
                throw new Exception("Process exited with return code " + process.ExitCode);
            }
        }

    }

}