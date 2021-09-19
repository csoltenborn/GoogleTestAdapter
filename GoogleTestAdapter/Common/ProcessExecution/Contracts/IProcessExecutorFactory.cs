namespace GoogleTestAdapter.Common.ProcessExecution.Contracts
{
    public interface IProcessExecutorFactory
    {
        IProcessExecutor CreateExecutor(bool printTestOutput, ILogger logger);
    }
}