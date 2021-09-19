using GoogleTestAdapter.Common.ProcessExecution.Contracts;

namespace GoogleTestAdapter.Common.ProcessExecution
{
    public class ProcessExecutorFactory : IProcessExecutorFactory
    {
        public IProcessExecutor CreateExecutor(bool printTestOutput, ILogger logger)
        {
            return new DotNetProcessExecutor(printTestOutput, logger);
        }
    }
}
