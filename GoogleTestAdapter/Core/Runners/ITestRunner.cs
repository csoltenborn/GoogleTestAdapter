using System.Collections.Generic;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.Runners
{

    public class ExecutableResult
    {
        public string Executable { get; set; }
        public int ResultCode { get; set; }
        public IList<string> ResultCodeOutput { get; set; }
        public bool ResultCodeSkip { get; set; }
    }

    public interface ITestRunner
    {
        void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, 
            IDebuggedProcessExecutorFactory processExecutorFactory);

        void Cancel();

        IList<ExecutableResult> ExecutableResults { get; }
    }

}