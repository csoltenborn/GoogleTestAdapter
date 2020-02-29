using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GoogleTestAdapter.Runners;

namespace GoogleTestAdapter.TestResults
{
    public class ExitCodeTestsAggregator : IExitCodeTestsAggregator
    {
        public IEnumerable<ExecutableResult> ComputeAggregatedResults(IEnumerable<ExecutableResult> allResults)
        {
            return allResults
                .GroupBy(r => r.Executable)
                .Select(results => new ExecutableResult(
                    executable: results.Key, 
                    exitCode: ComputeAggregatedExitCode(results),
                    exitCodeOutput: ComputeAggregatedOutput(results), 
                    exitCodeSkip: results.All(r => r.ExitCodeSkip)));
        }

        private List<string> ComputeAggregatedOutput(IEnumerable<ExecutableResult> results)
        {
            var completeOutput = new List<string>();
            foreach (ExecutableResult result in results)
            {
                if (result.ExitCodeOutput != null && result.ExitCodeOutput.Any(line => !string.IsNullOrWhiteSpace(line)))
                {
                    completeOutput.Add(Environment.NewLine);
                    completeOutput.AddRange(result.ExitCodeOutput);
                }
            }

            if (completeOutput.Any())
            {
                completeOutput.RemoveAt(0);
            }

            return completeOutput;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private int ComputeAggregatedExitCode(IEnumerable<ExecutableResult> results)
        {
            int minExitCode = results.Min(r => r.ExitCode);
            int maxExitCode = results.Max(r => r.ExitCode);
            return Math.Abs(maxExitCode) > Math.Abs(minExitCode)
                ? maxExitCode
                : minExitCode;
        }
    }
}