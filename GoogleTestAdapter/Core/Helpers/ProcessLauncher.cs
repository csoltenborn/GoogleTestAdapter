using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Helpers
{

    public class ProcessLauncher
    {
        private readonly ILogger Logger;
        private readonly string PathExtension;

        public ProcessLauncher(ILogger logger, string pathExtension)
        {
            Logger = logger;
            PathExtension = pathExtension;
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
            List<string> output = new List<string>();
            processExitCode = LaunchProcess(workingDirectory, command, param, printTestOutput, throwIfError, output);
            return output;
        }


        private int LaunchProcess(string workingDirectory, string command, string param, bool printTestOutput,
            bool throwIfError, List<string> output)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, param)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            if (!string.IsNullOrEmpty(PathExtension))
                processStartInfo.EnvironmentVariables["PATH"] = Utils.GetExtendedPath(PathExtension);

            Process process = Process.Start(processStartInfo);
            try
            {
                ProcessWaiter waiter = new ProcessWaiter(process);
                if (printTestOutput)
                {
                    Logger.LogInfo(
                        ">>>>>>>>>>>>>>> Output of command '" + command + " " + param + "'");
                }
                ReadTheStream(process, output, printTestOutput, throwIfError);
                if (printTestOutput)
                {
                    Logger.LogInfo("<<<<<<<<<<<<<<< End of Output");
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
                    Logger.LogInfo(line);
                }
            }
            if ((throwIfError && process.ExitCode != 0))
            {
                throw new Exception("Process exited with return code " + process.ExitCode);
            }
        }

    }

}