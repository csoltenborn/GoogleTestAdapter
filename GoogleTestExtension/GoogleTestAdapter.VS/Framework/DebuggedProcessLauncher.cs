using GoogleTestAdapter.Framework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Framework
{
    public class DebuggedProcessLauncher : IDebuggedProcessLauncher
    {
        private IFrameworkHandle FrameworkHandle { get; }

        public DebuggedProcessLauncher(IFrameworkHandle handle)
        {
            FrameworkHandle = handle;
        }

        public int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, string param)
        {
            return FrameworkHandle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, null);
        }
    }

}
