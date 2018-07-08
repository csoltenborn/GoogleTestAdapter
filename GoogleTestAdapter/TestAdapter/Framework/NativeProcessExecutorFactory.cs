using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class NativeProcessExecutorFactory : ProcessExecutorFactory, IDebuggedProcessExecutorFactory
    {
        private readonly IDebuggerAttacher _debuggerAttacher;

        public NativeProcessExecutorFactory(IDebuggerAttacher debuggerAttacher)
        {
            _debuggerAttacher = debuggerAttacher;
        }

        public IDebuggedProcessExecutor CreateDebuggingExecutor(bool printTestOutput, ILogger logger)
        {
            return new NativeDebuggedProcessExecutor(_debuggerAttacher, printTestOutput, logger);
        }
    }
}