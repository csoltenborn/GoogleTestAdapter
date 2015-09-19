using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Helpers
{
    static class ProcessUtils
    {

        internal static List<string> GetOutputOfCommand(IMessageLogger logger, string workingDirectory, string command, string param, bool printTestOutput, bool throwIfError, IRunContext runContext, IFrameworkHandle handle)
        {
            List<string> output = new List<string>();
            if (runContext != null && handle != null && runContext.IsBeingDebugged)
            {
                LaunchProcessWithDebuggerAttached(logger, workingDirectory, command, param, printTestOutput, handle);
            }
            else
            {
                LaunchProcess(logger, workingDirectory, command, param, printTestOutput, throwIfError, output);
            }
            return output;
        }

        private static void LaunchProcess(IMessageLogger logger, string workingDirectory, string command, string param,
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
                if (printTestOutput)
                {
                    logger.SendMessage(TestMessageLevel.Informational,
                        "GTA: >>>>>>>>>>>>>>> Output of command '" + command + " " + param + "'");
                }
                ReadTheStream(throwIfError, process, output, logger, printTestOutput);
                if (printTestOutput)
                {
                    logger.SendMessage(TestMessageLevel.Informational, "GTA: <<<<<<<<<<<<<<< End of Output");
                }
            }
            finally
            {
                process?.Dispose();
            }
        }

        private static void LaunchProcessWithDebuggerAttached(IMessageLogger logger, string workingDirectory, string command,
            string param, bool printTestOutput, IFrameworkHandle handle)
        {
            logger.SendMessage(TestMessageLevel.Informational, "GTA: Attaching debugger to " + command);
            if (printTestOutput)
            {
                DebugUtils.LogUserDebugMessage(logger, new GoogleTestAdapterOptions(),
                    TestMessageLevel.Informational,
                    "GTA: Note that due to restrictions of the VS Unit Testing framework, the test executable's output can not be displayed in the test console when debugging tests!");
            }
            Process process =
                Process.GetProcessById(handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param,
                    null));
            process.WaitForExit();
        }

        // ReSharper disable once UnusedParameter.Local
        private static void ReadTheStream(bool throwIfError, Process process, List<string> streamContent, IMessageLogger logger, bool printTestOutput)
        {
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                streamContent.Add(line);
                if (printTestOutput)
                {
                    logger.SendMessage(TestMessageLevel.Informational, line);
                }
            }
            if ((throwIfError && process.ExitCode != 0))
            {
                throw new Exception("Process exited with return code " + process.ExitCode);
            }
        }

    }

}