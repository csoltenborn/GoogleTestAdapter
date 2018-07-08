using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.ProcessExecution
{
    public class NativeDebuggedProcessExecutorFactory : ProcessExecutorFactory, IDebuggedProcessExecutorFactory
    {
        private readonly IDebuggerAttacher _debuggerAttacher;

        public NativeDebuggedProcessExecutorFactory(IDebuggerAttacher debuggerAttacher)
        {
            _debuggerAttacher = debuggerAttacher;
        }

        public IDebuggedProcessExecutor CreateDebuggingExecutor(bool printTestOutput, ILogger logger)
        {
            return new NativeDebuggedProcessExecutor(_debuggerAttacher, printTestOutput, logger);
        }
    }
}