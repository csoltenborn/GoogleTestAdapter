using System.Collections.Concurrent;
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

            var localStandardOut = new ConcurrentQueue<string>();
            var localStandardErr = new ConcurrentQueue<string>();
            var localAllOutput = new ConcurrentQueue<string>();
            process.OutputDataReceived += (sender, e) =>
            {
                localStandardOut.Enqueue(e.Data);
                localAllOutput.Enqueue(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                localStandardErr.Enqueue(e.Data);
                localAllOutput.Enqueue(e.Data);
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