// This file has been modified by Microsoft on 8/2017.

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
            _logger.DebugInfo(Resources.SchedulingStats);
            _logger.DebugInfo(String.Format(Resources.ExpectedTestCase, ExpectedTestcaseDurations.Count));
            _logger.DebugInfo(String.Format(Resources.ActualTestCase, ActualTestcaseDurations.Count));
            if (ExpectedTestcaseDurations.Count == 0 || ActualTestcaseDurations.Count == 0)
            {
                _logger.DebugInfo(Resources.NothingToReport);
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

            _logger.DebugInfo(String.Format(Resources.ExpectedDurations, differences.Count));
            _logger.DebugInfo(String.Format(Resources.AvgDifference, avgDifference.ToString("F1", CultureInfo.InvariantCulture)));
            _logger.DebugInfo(String.Format(Resources.StandardDeviation, standardDeviation.ToString("F1", CultureInfo.InvariantCulture)));

            int nrOfWorstDifferences = Math.Min(10, differences.Count);
            _logger.DebugInfo(String.Format(Resources.WorstDifferences, nrOfWorstDifferences));
            for (int i = 0; i < nrOfWorstDifferences; i++)
            {
                _logger.DebugInfo(String.Format(Resources.Results, differences[i].TestCase.FullyQualifiedName, ExpectedTestcaseDurations[differences[i].TestCase], ActualTestcaseDurations[differences[i].TestCase]));
            }
        }

    }

}