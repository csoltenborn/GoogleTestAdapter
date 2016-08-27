using System;

namespace GoogleTestAdapter.Framework
{

    public interface IProcessExecutor
    {
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, Action<string> reportOutputLine);
    }

    // ReSharper disable once InconsistentNaming
    public static class IProcessExecutorExtensions
    {
        public static int ExecuteBatchFileBlocking(this IProcessExecutor executor, string batchFile, string parameters, string workingDir, string pathExtension, Action<string> reportOutputLine)
        {
            return executor.ExecuteCommandBlocking($"cmd.exe /C {batchFile}", parameters, workingDir, pathExtension,
                reportOutputLine);
        }
    }

}