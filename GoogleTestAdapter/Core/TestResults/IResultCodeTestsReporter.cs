using System.Collections.Generic;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter.TestResults
{
    public interface IResultCodeTestsReporter
    {
        void ReportResultCodeTestCases(IEnumerable<ExecutableResult> allResults, bool isBeingDebugged);
    }
}