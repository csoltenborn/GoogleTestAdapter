using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.Helpers
{

    class ProcessLauncher
    {
        private TestEnvironment TestEnvironment { get; }

        internal ProcessLauncher(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }

        internal List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput, bool throwIfError, IRunContext runContext, IFrameworkHandle handle)
        {
            int dummy;
            return GetOutputOfCommand(workingDirectory, command, param, printTestOutput, throwIfError, runContext, handle, out dummy);
        }

        internal List<string> GetOutputOfCommand(string workingDirectory, string command, string param, bool printTestOutput, bool throwIfError, IRunContext runContext, IFrameworkHandle handle, out int processExitCode)
        {
            List<string> output = new List<string>();
            if (runContext != null && handle != null && runContext.IsBeingDebugged)
            {
                processExitCode = LaunchProcessWithDebuggerAttached(workingDirectory, command, param, printTestOutput, handle);
            }
            else
            {
                processExitCode = LaunchProcess(workingDirectory, command, param, printTestOutput, throwIfError, output);
            }
            return output;
        }

        private int LaunchProcess(string workingDirectory, string command, string param,
            bool printTestOutput, bool throwIfError, List<string> output)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, param)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            Process process = Process.Start(processStartInfo);
            try
            {
                ProcessWaiter waiter = new ProcessWaiter(process);
                if (printTestOutput)
                {
                    TestEnvironment.LogInfo(
                        ">>>>>>>>>>>>>>> Output of command '" + command + " " + param + "'");
                }
                ReadTheStream(process, output, printTestOutput, throwIfError);
                if (printTestOutput)
                {
                    TestEnvironment.LogInfo("<<<<<<<<<<<<<<< End of Output");
                }
                waiter.WaitForExit();
                return waiter.ProcessExitCode;
            }
            finally
            {
                process?.Dispose();
            }
        }

        private int LaunchProcessWithDebuggerAttached(string workingDirectory, string command,
            string param, bool printTestOutput, IFrameworkHandle handle)
        {
            TestEnvironment.LogInfo("Attaching debugger to " + command);
            if (printTestOutput)
            {
                TestEnvironment.LogInfo(
                    "Note that due to restrictions of the VS Unit Testing framework, the test executable's output can not be displayed in the test console when debugging tests!",
                    TestEnvironment.LogType.UserDebug);
            }
            int processId = handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param,
                null);
            Process process = Process.GetProcessById(processId);
            ProcessWaiter waiter = new ProcessWaiter(process);
            waiter.WaitForExit();
            process.Dispose();
            return waiter.ProcessExitCode;
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
                    TestEnvironment.LogInfo(line);
                }
            }
            if ((throwIfError && process.ExitCode != 0))
            {
                throw new Exception("Process exited with return code " + process.ExitCode);
            }
        }

    }

}