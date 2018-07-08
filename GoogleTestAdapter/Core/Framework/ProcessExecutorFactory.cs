using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    public class ProcessExecutorFactory : IProcessExecutorFactory
    {
        public IProcessExecutor CreateExecutor(bool printTestOutput, ILogger logger, Action<int> reportProcessId = null)
        {
            return new FrameworkProcessExecutor(printTestOutput, logger, reportProcessId);
        }
    }
}
