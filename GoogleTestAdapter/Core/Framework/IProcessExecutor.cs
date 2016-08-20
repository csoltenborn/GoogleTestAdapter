using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Framework
{
    public interface IProcessExecutor
    {
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, Action<string> reportStandardOutputLine, Action<string> reportStandardErrorLine);
    }

    // ReSharper disable once InconsistentNaming
    public static class IProcessExecutorExtensions
    {

        public static int ExecuteCommandBlocking(this IProcessExecutor processExecutor, string command, string parameters, string workingDir,
            string pathExtension, out string[] standardOutput, out string[] errorOutput)
        {
            var standardOutputLines = new List<string>();
            var errorOutputLines = new List<string>();

            int exitCode = processExecutor.ExecuteCommandBlocking(
                command, parameters, workingDir, pathExtension,
                s => standardOutputLines.Add(s),
                s => errorOutputLines.Add(s));

            standardOutput = standardOutputLines.ToArray();
            errorOutput = errorOutputLines.ToArray();

            return exitCode;
        }
    }

}