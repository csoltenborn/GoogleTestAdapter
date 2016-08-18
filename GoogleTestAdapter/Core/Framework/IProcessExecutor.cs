using System;

namespace GoogleTestAdapter.Framework
{
    public interface IProcessExecutor
    {
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, out string[] standardOutput, out string[] errorOutput);
        int ExecuteCommandBlocking(string command, string parameters, string workingDir, Action<string> reportStandardOutputLine, Action<string> reportStandardErrorLine);
    }
}