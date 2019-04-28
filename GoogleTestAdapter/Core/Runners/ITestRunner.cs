using System.Collections.Generic;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution.Contracts;

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