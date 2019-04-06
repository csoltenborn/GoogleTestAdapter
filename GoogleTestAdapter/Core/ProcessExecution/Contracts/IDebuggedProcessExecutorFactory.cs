using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IDebuggedProcessExecutorFactory : IProcessExecutorFactory
    {
        IDebuggedProcessExecutor CreateFrameworkDebuggingExecutor(bool printTestOutput, ILogger logger);

        IDebuggedProcessExecutor CreateNativeDebuggingExecutor(DebuggerEngine debuggerEngine, bool printTestOutput, ILogger logger);
    }
}
