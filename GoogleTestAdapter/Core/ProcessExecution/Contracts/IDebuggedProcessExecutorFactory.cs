using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IDebuggedProcessExecutorFactory : IProcessExecutorFactory
    {
        IDebuggedProcessExecutor CreateDebuggingExecutor(bool printTestOutput, ILogger logger);
    }
}
