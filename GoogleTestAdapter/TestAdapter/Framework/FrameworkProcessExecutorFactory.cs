using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class FrameworkProcessExecutorFactory : ProcessExecutorFactory, IDebuggedProcessExecutorFactory
    {
        private readonly IFrameworkHandle _frameworkHandle;

        public FrameworkProcessExecutorFactory(IFrameworkHandle frameworkHandle)
        {
            _frameworkHandle = frameworkHandle;
        }

        public IDebuggedProcessExecutor CreateDebuggingExecutor(bool printTestOutput, ILogger logger)
        {
            return new FrameworkDebuggedProcessExecutor(_frameworkHandle, printTestOutput, logger);
        }
    }
}