using System;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.ProcessExecution.Contracts
{
    public interface IProcessExecutorFactory
    {
        IProcessExecutor CreateExecutor(bool printTestOutput, ILogger logger, Action<int> reportProcessId = null);
    }
}
