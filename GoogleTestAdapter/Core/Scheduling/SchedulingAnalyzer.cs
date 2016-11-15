using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GoogleTestAdapter.Helpers;
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


        private readonly TestEnvironment _testEnvironment;

        public SchedulingAnalyzer(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
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
            _testEnvironment.Logger.DebugInfo(">>> Scheduling statistics <<<");
            _testEnvironment.Logger.DebugInfo($"# of expected test case durations: {ExpectedTestcaseDurations.Count}");
            _testEnvironment.Logger.DebugInfo($"# of actual test case durations: {ActualTestcaseDurations.Count}");
            if (ExpectedTestcaseDurations.Count == 0 || ActualTestcaseDurations.Count == 0)
            {
                _testEnvironment.Logger.DebugInfo("Nothing to report.");
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

            _testEnvironment.Logger.DebugInfo($"{differences.Count} expected durations have been found in actual durations");
            _testEnvironment.Logger.DebugInfo($"Avg difference between expected and actual duration: {avgDifference.ToString("F1", CultureInfo.InvariantCulture)}ms");
            _testEnvironment.Logger.DebugInfo($"Standard deviation: {standardDeviation.ToString("F1", CultureInfo.InvariantCulture)}ms");

            int nrOfWorstDifferences = Math.Min(10, differences.Count);
            _testEnvironment.Logger.DebugInfo($"{nrOfWorstDifferences} worst differences:");
            for (int i = 0; i < nrOfWorstDifferences; i++)
            {
                _testEnvironment.Logger.DebugInfo($"Test {differences[i].TestCase.FullyQualifiedName}: Expected {ExpectedTestcaseDurations[differences[i].TestCase]}ms, actual {ActualTestcaseDurations[differences[i].TestCase]}ms");
            }
        }

    }

}