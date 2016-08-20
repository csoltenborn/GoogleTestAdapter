using System;

namespace GoogleTestAdapter.Framework
{
    public interface IProcessExecutor
    {
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, out string[] standardOutput, out string[] errorOutput);
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension, Action<string> reportStandardOutputLine, Action<string> reportStandardErrorLine);
    }
}