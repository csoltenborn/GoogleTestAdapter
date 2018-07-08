using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.ProcessExecution.Contracts;
using GoogleTestAdapter.Settings;
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

        public IDebuggedProcessExecutor CreateDebuggingExecutor(SettingsWrapper settings, bool printTestOutput, ILogger logger)
        {
            if (settings.UseNewTestExecutionFramework)
            {
                return new NativeDebuggedProcessExecutor(_debuggerAttacher, printTestOutput, logger);
            }
            
            return new FrameworkDebuggedProcessExecutor(_frameworkHandle, printTestOutput, logger);
        }
    }
}