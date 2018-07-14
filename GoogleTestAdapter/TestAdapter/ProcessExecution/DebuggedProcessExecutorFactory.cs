using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    public class DebuggedProcessExecutorFactory : ProcessExecutorFactory, IDebuggedProcessExecutorFactory
    {
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly IDebuggerAttacher _debuggerAttacher;

        public DebuggedProcessExecutorFactory(IFrameworkHandle frameworkHandle, IDebuggerAttacher debuggerAttacher)
        {
            _frameworkHandle = frameworkHandle;
            _debuggerAttacher = debuggerAttacher;
        }

        public IDebuggedProcessExecutor CreateNativeDebuggingExecutor(bool printTestOutput, ILogger logger)
        {
            return new NativeDebuggedProcessExecutor(_debuggerAttacher, printTestOutput, logger);
        }

        public IDebuggedProcessExecutor CreateFrameworkDebuggingExecutor(bool printTestOutput, ILogger logger)
        {
            return new FrameworkDebuggedProcessExecutor(_frameworkHandle, printTestOutput, logger);
        }
    }
}