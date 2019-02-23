using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter.TestResults
{
    public class ResultCodeTestsAggregator : IResultCodeTestsAggregator
    {
        public IEnumerable<ExecutableResult> ComputeAggregatedResults(IEnumerable<ExecutableResult> allResults)
        {
            return allResults
                .GroupBy(r => r.Executable)
                .Select(results => new ExecutableResult
                {
                    Executable = results.Key,
                    ResultCode = ComputeAggregatedResultCode(results),
                    ResultCodeOutput = ComputeAggregatedOutput(results),
                    ResultCodeSkip = results.All(r => r.ResultCodeSkip)
                });
        }

        private List<string> ComputeAggregatedOutput(IEnumerable<ExecutableResult> results)
        {
            var completeOutput = new List<string>();
            foreach (ExecutableResult result in results)
            {
                completeOutput.Add(Environment.NewLine);
                completeOutput.AddRange(result.ResultCodeOutput);
            }

            completeOutput.RemoveAt(0);
            return completeOutput;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private int ComputeAggregatedResultCode(IEnumerable<ExecutableResult> results)
        {
            int minResultCode = results.Min(r => r.ResultCode);
            int maxResultCode = results.Max(r => r.ResultCode);
            return Math.Abs(maxResultCode) > Math.Abs(minResultCode)
                ? maxResultCode
                : minResultCode;
        }
    }
}