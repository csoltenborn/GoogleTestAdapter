using System.Collections.Generic;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class DebuggedProcessLauncher : IDebuggedProcessLauncher
    {
        private readonly IFrameworkHandle _frameworkHandle;

        public DebuggedProcessLauncher(IFrameworkHandle handle)
        {
            _frameworkHandle = handle;
        }

        public int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, string param, string pathExtension)
        {
            IDictionary<string, string> envVariables = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(pathExtension))
                envVariables["PATH"] = Utils.GetExtendedPath(pathExtension);
            return _frameworkHandle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, envVariables);
        }
    }

}
