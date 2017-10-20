// This file has been modified by Microsoft on 6/2017.

using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Scheduling
{
    public class DurationBasedTestsSplitter : ITestsSplitter
    {
        private readonly int _overallDuration;
        private readonly IDictionary<TestCase, int> _testcaseDurations;
        private readonly SettingsWrapper _settings;


        public DurationBasedTestsSplitter(IDictionary<TestCase, int> testcaseDurations, SettingsWrapper settings)
        {
            _settings = settings;
            _testcaseDurations = testcaseDurations;
            _overallDuration = testcaseDurations.Values.Sum();
        }


        public List<List<TestCase>> SplitTestcases()
        {
            List<TestCase> sortedTestcases = _testcaseDurations.Keys.OrderByDescending(tc => _testcaseDurations[tc]).ToList();
            int nrOfThreadsToUse = _settings.MaxNrOfThreads;
            int targetDuration = _overallDuration / nrOfThreadsToUse;

            var splitTestcases = new List<List<TestCase>>();
            var currentList = new List<TestCase>();
            int currentDuration = 0;
            while (sortedTestcases.Count > 0 && splitTestcases.Count < nrOfThreadsToUse)
            {
                do
                {
                    TestCase testcase = sortedTestcases[0];

                    sortedTestcases.RemoveAt(0);
                    currentList.Add(testcase);
                    currentDuration += _testcaseDurations[testcase];
                } while (sortedTestcases.Count > 0 && currentDuration <= targetDuration - _testcaseDurations[sortedTestcases[0]]);

                splitTestcases.Add(currentList);
                currentList = new List<TestCase>();
                currentDuration = 0;
            }

            while (sortedTestcases.Count > 0)
            {
                // TODO performance
                int index = GetIndexOfListWithShortestDuration(splitTestcases);
                splitTestcases[index].Add(sortedTestcases[0]);
                sortedTestcases.RemoveAt(0);
            }

            return splitTestcases;
        }


        private int GetIndexOfListWithShortestDuration(List<List<TestCase>> splitTestcases)
        {
            int index = 0;
            int minDuration = int.MaxValue;
            for (int i = 0; i < splitTestcases.Count; i++)
            {
                List<TestCase> testcases = splitTestcases[i];
                int duration = testcases.Sum(tc => _testcaseDurations[tc]);
                if (duration < minDuration)
                {
                    minDuration = duration;
                    index = i;
                }
            }
            return index;
        }

    }

}