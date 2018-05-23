// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleTestAdapter.Framework
{

    public interface IProcessExecutor
    {
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, IDictionary<string, string> envVars, string pathExtension, Action<string> reportOutputLine);
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
            return executor.ExecuteCommandBlocking(command, $"/C \"{batchFile}\" {parameters}", workingDir, null, pathExtension,
                reportOutputLine);
        }
    }

}