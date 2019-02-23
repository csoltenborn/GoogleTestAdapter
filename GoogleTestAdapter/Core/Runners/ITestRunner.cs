using System;
using System.Collections.Generic;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.Runners
{

    public class ExecutableResult
    {
        public string Executable { get; }
        public int ResultCode { get; }
        public IReadOnlyList<string> ResultCodeOutput { get; }
        public bool ResultCodeSkip { get; }

        public ExecutableResult(string executable, int resultCode = 0, IList<string> resultCodeOutput = null, bool resultCodeSkip = false)
        {
            if (string.IsNullOrWhiteSpace(executable))
            {
                throw new ArgumentException(nameof(executable));
            }

            Executable = executable;
            ResultCode = resultCode;
            ResultCodeOutput = (IReadOnlyList<string>) (resultCodeOutput ?? new List<string>());
            ResultCodeSkip = resultCodeSkip;
        }
    }

    public interface ITestRunner
    {
        void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, 
            IDebuggedProcessExecutorFactory processExecutorFactory);

        void Cancel();

        IList<ExecutableResult> ExecutableResults { get; }
    }

}