using System.Collections.Generic;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter.TestResults
{
    public interface IResultCodeTestsAggregator
    {
        IEnumerable<ExecutableResult> ComputeAggregatedResults(IEnumerable<ExecutableResult> allResults);
    }
}