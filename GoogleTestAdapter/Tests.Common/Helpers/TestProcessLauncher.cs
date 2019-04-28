using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Tests.Common.Helpers
{

    public class TestProcessLauncher
    {

        public int GetOutputStreams(string workingDirectory, string command, string param, out List<string> standardOut,
            out List<string> standardErr)
        {
            return GetOutputStreams(workingDirectory, command, param, out standardOut, out standardErr, out _);
        }

        public int GetOutputStreams(string workingDirectory, string command, string param, out List<string> standardOut, out List<string> standardErr, out List<string> allOutput)
        {
            Process process = CreateProcess(workingDirectory, command, param);

            var localStandardOut = new List<string>();
            var localStandardErr = new List<string>();
            var localAllOutput = new List<string>();
            process.OutputDataReceived += (sender, e) =>
            {
                localStandardOut.Add(e.Data);
                localAllOutput.Add(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                localStandardErr.Add(e.Data);
                localAllOutput.Add(e.Data);
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var waiter = new ProcessWaiter(process);
                waiter.WaitForExit();

                standardOut = localStandardOut.Where(s => s != null).ToList();
                standardErr = localStandardErr.Where(s => s != null).ToList();
                allOutput = localAllOutput.Where(s => s != null).ToList();
                return waiter.ProcessExitCode;
            }
            finally
            {
                process.Dispose();
            }
        }

        private Process CreateProcess(string workingDirectory, string command, string param)
        {
            var processStartInfo = new ProcessStartInfo(command, param)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            return new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
        }

    }

}