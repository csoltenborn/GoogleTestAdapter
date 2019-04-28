using System.Collections.Generic;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter.TestResults
{
    public interface IExitCodeTestsReporter
    {
        void ReportExitCodeTestCases(IEnumerable<ExecutableResult> allResults, bool isBeingDebugged);
    }
}