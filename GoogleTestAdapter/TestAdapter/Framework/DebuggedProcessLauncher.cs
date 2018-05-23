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
            return LaunchProcessWithDebuggerAttached(command, workingDirectory, null, param, pathExtension);
        }

        public int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, IDictionary<string, string> additionalEnvVars, string param, string pathExtension)
        {
            IDictionary<string, string> envVariables = new Dictionary<string, string>();
            if (additionalEnvVars != null)
            {
                foreach (var envVar in additionalEnvVars)
                {
                    envVariables[envVar.Key] = envVar.Value;
                }
            }

            if (!string.IsNullOrEmpty(pathExtension))
            {
                var path = Utils.GetExtendedPath(pathExtension);
                if (envVariables.ContainsKey("PATH"))
                {
                    path += $";{envVariables["PATH"]}";
                }
                envVariables["PATH"] = path;
            }

            return _frameworkHandle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, envVariables);
        }
    }

}
