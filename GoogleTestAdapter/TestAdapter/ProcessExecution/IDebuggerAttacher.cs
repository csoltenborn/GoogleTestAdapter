using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    public interface IDebuggerAttacher
    {
        bool AttachDebugger(int processId, DebuggerEngine debuggerEngine);
    }
}