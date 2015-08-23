using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace GoogleTestAdapter
{
    class ProcessUtils
    {

        /*
                    if (runContext.IsBeingDebugged)
            {
                handle.SendMessage(TestMessageLevel.Informational, "Attaching debugger to " + executable);
                Process.GetProcessById(handle.LaunchProcessWithDebuggerAttached(executable, WorkingDir, Arguments, null)).WaitForExit();
    }
            else
            {
                handle.SendMessage(TestMessageLevel.Informational, "In " + WorkingDir + ", running: " + executable + " " + Arguments);
                consoleOutput = ProcessUtils.GetOutputOfCommand(handle, WorkingDir, executable, Arguments);
            }
            */


        public static List<string> GetOutputOfCommand(IMessageLogger logger, string workingDirectory, string command, string param, bool throwIfError = false)
        {
            List<string> output = new List<string>();
            if (!File.Exists(command))
            {
                logger.SendMessage(TestMessageLevel.Error, "Ignoring executable because it does not exist: " + command);
                return output;
            }

            try
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
                    ReadTheStream(throwIfError, process, output);
                }
                finally
                {
                    process.Dispose();
                }
            }
            catch (Win32Exception e)
            {
                logger.SendMessage(TestMessageLevel.Error, "Error occured during process start, message: " + e.ToString());
            }

            return output;
        }

        private static List<string> ReadTheStream(bool throwIfError, Process process, List<string> streamContent)
        {
            while (!process.StandardOutput.EndOfStream)
            {
                streamContent.Add(process.StandardOutput.ReadLine());
            }
            if ((!throwIfError ? false : process.ExitCode != 0))
            {
                throw new Exception("Process exited with return code " + process.ExitCode);
            }
            return streamContent;
        }

        public static void RunCommand(string wd, string command, string param)
        {
            GetOutputOfCommand(new ConsoleLogger(), wd, command, param);
        }

    }

}
