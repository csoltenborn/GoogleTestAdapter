using GoogleTestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Helpers
{
    class DebuggedProcessLauncher : IDebuggedProcessLauncher
    {
        private IFrameworkHandle FrameworkHandle { get; }

        internal DebuggedProcessLauncher(IFrameworkHandle handle)
        {
            FrameworkHandle = handle;
        }

        public int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, string param)
        {
            return FrameworkHandle.LaunchProcessWithDebuggerAttached(command, workingDirectory, param, null);
        }
    }

}
