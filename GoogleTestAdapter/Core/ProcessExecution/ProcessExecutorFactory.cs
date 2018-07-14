using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.ProcessExecution
{
    public class ProcessExecutorFactory : IProcessExecutorFactory
    {
        public IProcessExecutor CreateExecutor(bool printTestOutput, ILogger logger)
        {
            return new DotNetProcessExecutor(printTestOutput, logger);
        }
    }
}
