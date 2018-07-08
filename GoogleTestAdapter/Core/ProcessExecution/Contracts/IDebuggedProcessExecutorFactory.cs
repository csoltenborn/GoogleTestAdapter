using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IDebuggedProcessExecutorFactory : IProcessExecutorFactory
    {
        IDebuggedProcessExecutor CreateDebuggingExecutor(SettingsWrapper settings, bool printTestOutput, ILogger logger);
    }
}
