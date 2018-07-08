using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    public class FrameworkDebuggedProcessExecutorFactory : ProcessExecutorFactory, IDebuggedProcessExecutorFactory
    {
        private readonly IFrameworkHandle _frameworkHandle;

        public FrameworkDebuggedProcessExecutorFactory(IFrameworkHandle frameworkHandle)
        {
            _frameworkHandle = frameworkHandle;
        }

        public IDebuggedProcessExecutor CreateDebuggingExecutor(bool printTestOutput, ILogger logger)
        {
            return new FrameworkDebuggedProcessExecutor(_frameworkHandle, printTestOutput, logger);
        }
    }
}