using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace GoogleTestAdapter
{
    public static class ProcessUtils
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


        public static List<string> GetOutputOfCommand(IMessageLogger logger, string workingDirectory, string command, string param, bool printTestOutput, bool throwIfError)
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
                    process.Dispose();
                }
            }
            catch (Win32Exception e)
            {
                logger.SendMessage(TestMessageLevel.Error, "Error occured during process start, message: " + e.ToString());
            }

            return output;
        }

        private static List<string> ReadTheStream(bool throwIfError, Process process, List<string> streamContent, IMessageLogger logger, bool printTestOutput)
        {
            while (!process.StandardOutput.EndOfStream)
            {
                string Line = process.StandardOutput.ReadLine();
                streamContent.Add(Line);
                if (printTestOutput)
                {
                    logger.SendMessage(TestMessageLevel.Informational, Line);
                }
            }
            if ((!throwIfError ? false : process.ExitCode != 0))
            {
                throw new Exception("Process exited with return code " + process.ExitCode);
            }
            return streamContent;
        }

        public static Process VisualStudioMainProcess
        {
            get
            {
                Process TheProcess = Process.GetCurrentProcess();
                while (TheProcess != null && !TheProcess.ProcessName.ToLower().Contains("devenv"))
                {
                    TheProcess = TheProcess.Parent();
                }
                return TheProcess;
            }
        }

    }

    // from http://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
    public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if ((int)processId.NextValue() == pid)
                {
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int)parentId.NextValue());
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
    }

}