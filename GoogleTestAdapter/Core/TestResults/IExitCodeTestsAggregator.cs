using System.Collections.Generic;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter.TestResults
{
    public interface IExitCodeTestsAggregator
    {
        IEnumerable<ExecutableResult> ComputeAggregatedResults(IEnumerable<ExecutableResult> allResults);
    }
}