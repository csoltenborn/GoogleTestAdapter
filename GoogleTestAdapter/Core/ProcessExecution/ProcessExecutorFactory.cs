using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.ProcessExecution
{
    public class ProcessExecutorFactory : IProcessExecutorFactory
    {
        public IProcessExecutor CreateExecutor(bool printTestOutput, ILogger logger, Action<int> reportProcessId = null)
        {
            return new FrameworkProcessExecutor(printTestOutput, logger, reportProcessId);
        }
    }
}
