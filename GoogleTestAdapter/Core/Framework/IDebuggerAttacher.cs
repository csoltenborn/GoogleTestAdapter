namespace GoogleTestAdapter.Framework
{
    public interface IDebuggerAttacher
    {
        bool AttachDebugger(int processId);
    }
}