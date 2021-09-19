using System.Collections.Generic;
using GoogleTestAdapter.Common.ProcessExecution.Contracts;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Runners
{
    public interface ITestRunner
    {
        void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, 
            IDebuggedProcessExecutorFactory processExecutorFactory);

        void Cancel();

        IList<ExecutableResult> ExecutableResults { get; }
    }

}