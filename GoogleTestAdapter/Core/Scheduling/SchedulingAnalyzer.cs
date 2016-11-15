using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Scheduling
{
    public class SchedulingAnalyzer
    {
        private class Difference
        {
            public TestCase TestCase;
            public int DifferenceInMs;
        }


        private readonly ILogger _logger;

        public SchedulingAnalyzer(ILogger logger)
        {
            _logger = logger;
        }

        private ConcurrentDictionary<TestCase, int> ExpectedTestcaseDurations { get; } = new ConcurrentDictionary<TestCase, int>();
        private ConcurrentDictionary<TestCase, int> ActualTestcaseDurations { get; } = new ConcurrentDictionary<TestCase, int>();

        public bool AddExpectedDuration(TestCase testCase, int duration)
        {
            return ExpectedTestcaseDurations.TryAdd(testCase, duration);
        }

        public bool AddActualDuration(TestCase testCase, int duration)
        {
            return ActualTestcaseDurations.TryAdd(testCase, duration);
        }

        public void PrintStatisticsToDebugOutput()
        {
            _logger.DebugInfo(">>> Scheduling statistics <<<");
            _logger.DebugInfo($"# of expected test case durations: {ExpectedTestcaseDurations.Count}");
            _logger.DebugInfo($"# of actual test case durations: {ActualTestcaseDurations.Count}");
            if (ExpectedTestcaseDurations.Count == 0 || ActualTestcaseDurations.Count == 0)
            {
                _logger.DebugInfo("Nothing to report.");
                return;
            }

            var differences = new List<Difference>();
            differences.AddRange(ExpectedTestcaseDurations
                .Where(ed => ActualTestcaseDurations.ContainsKey(ed.Key))
                .Select(ed => new Difference
                    {
                        TestCase = ed.Key,
                        DifferenceInMs = ed.Value - ActualTestcaseDurations[ed.Key]
                    }));
            differences.Sort((d1, d2) => Math.Abs(d2.DifferenceInMs) - Math.Abs(d1.DifferenceInMs));

            int sumOfAllDifferences = differences.Select(d => d.DifferenceInMs).Sum();
            double avgDifference = (double) sumOfAllDifferences / differences.Count;
            double sumOfSquaresOfDifferences = differences.Select(d => (d.DifferenceInMs - avgDifference) * (d.DifferenceInMs - avgDifference)).Sum();
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / differences.Count);

            _logger.DebugInfo($"{differences.Count} expected durations have been found in actual durations");
            _logger.DebugInfo($"Avg difference between expected and actual duration: {avgDifference.ToString("F1", CultureInfo.InvariantCulture)}ms");
            _logger.DebugInfo($"Standard deviation: {standardDeviation.ToString("F1", CultureInfo.InvariantCulture)}ms");

            int nrOfWorstDifferences = Math.Min(10, differences.Count);
            _logger.DebugInfo($"{nrOfWorstDifferences} worst differences:");
            for (int i = 0; i < nrOfWorstDifferences; i++)
            {
                _logger.DebugInfo($"Test {differences[i].TestCase.FullyQualifiedName}: Expected {ExpectedTestcaseDurations[differences[i].TestCase]}ms, actual {ActualTestcaseDurations[differences[i].TestCase]}ms");
            }
        }

    }

}