using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Framework
{
    public interface IDebuggedProcessExecutorFactory
    {
        IDebuggedProcessExecutor CreateDebuggingExecutor(bool printTestOutput, ILogger logger);
    }
}
