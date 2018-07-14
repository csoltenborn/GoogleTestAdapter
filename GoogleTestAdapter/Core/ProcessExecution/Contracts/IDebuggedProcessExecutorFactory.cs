using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IDebuggedProcessExecutorFactory : IProcessExecutorFactory
    {
        IDebuggedProcessExecutor CreateFrameworkDebuggingExecutor(bool printTestOutput, ILogger logger);

        IDebuggedProcessExecutor CreateNativeDebuggingExecutor(bool printTestOutput, ILogger logger);
    }
}
