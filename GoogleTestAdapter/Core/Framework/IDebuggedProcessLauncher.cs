using System.Collections.Generic;

namespace GoogleTestAdapter.Framework
{

    public interface IDebuggedProcessLauncher
    {
        int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, IDictionary<string, string> additionalEnvVars, string param, string pathExtension);
    }

}