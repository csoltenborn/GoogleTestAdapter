// This file has been modified by Microsoft on 6/2017.

using System;
using System.IO;
using System.Collections.Generic;

namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IProcessExecutor
    {
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension,  IDictionary<string, string> environmentVariables, Action<string> reportOutputLine);
        void Cancel();
    }

    // ReSharper disable once InconsistentNaming
    public static class IProcessExecutorExtensions
    {
        public static int ExecuteBatchFileBlocking(this IProcessExecutor executor, string batchFile, string parameters, string workingDir, string pathExtension, Action<string> reportOutputLine)
        {
            if (!File.Exists(batchFile))
            {
                throw new FileNotFoundException("File not found", batchFile);
            }

            string command = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            return executor.ExecuteCommandBlocking(command, $"/C \"{batchFile}\" {parameters}", workingDir, pathExtension, new Dictionary<string, string>(),
                reportOutputLine);
        }
    }

}