namespace GoogleTestAdapter.Framework
{

    public interface IDebuggedProcessLauncher
    {
        int LaunchProcessWithDebuggerAttached(string command, string workingDirectory, string param, string pathExtension);
    }

}