namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IDebuggerAttacher
    {
        bool AttachDebugger(int processId);
    }
}