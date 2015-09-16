using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.Helpers
{
    public static class ProcessUtils
    {


        public static List<string> GetOutputOfCommand(IMessageLogger logger, string workingDirectory, string command, string param, bool printTestOutput, bool throwIfError, IRunContext runContext, IFrameworkHandle handle)
        {
            List<string> output = new List<string>();
            try
            {
                Process process;
                if (runContext != null && handle != null && runContext.IsBeingDebugged)
                {
                    logger.SendMessage(TestMessageLevel.Informational, "Attaching debugger to " + command);
                    if (printTestOutput)
                    {
                        logger.SendMessage(TestMessageLevel.Informational, "Note that because of restrictions of the VS Unit Test framework, the test executable's output can not be displayed in the test console when debugging tests!");
                    }
                    process =
                        Process.GetProcessById(handle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param,
                            null));
                    process.WaitForExit();
                }
                else
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo(command, param)
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = false,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory
                    };
                    process = Process.Start(processStartInfo);
                    try
                    {
                        if (printTestOutput)
                        {
                            logger.SendMessage(TestMessageLevel.Informational, ">>>>>>>>>>>>>>> Output of command '" + command + " " + param + "'");
                        }
                        ReadTheStream(throwIfError, process, output, logger, printTestOutput);
                        if (printTestOutput)
                        {
                            logger.SendMessage(TestMessageLevel.Informational, "<<<<<<<<<<<<<<< End of Output");
                        }
                    }
                    finally
                    {
                        process?.Dispose();
                    }
                }
            }
            catch (Win32Exception e)
            {
                logger.SendMessage(TestMessageLevel.Error, "Error occured during process start, message: " + e);
            }

            return output;
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