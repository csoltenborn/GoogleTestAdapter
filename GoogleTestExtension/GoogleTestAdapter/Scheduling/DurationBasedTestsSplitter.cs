using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Scheduling
{
    public class DurationBasedTestsSplitter : AbstractGoogleTestAdapterClass, ITestsSplitter
    {
        private readonly int overallDuration;
        private readonly IDictionary<TestCase, int> testcaseDurations;

        public DurationBasedTestsSplitter(IDictionary<TestCase, int> testcaseDurations) : this(testcaseDurations, null) { }

        public DurationBasedTestsSplitter(IDictionary<TestCase, int> testcaseDurations, IOptions options) : base(options)
        {
            this.testcaseDurations = testcaseDurations;
            this.overallDuration = testcaseDurations.Values.Sum();
        }

        public List<List<TestCase>> SplitTestcases()
        {
            List<TestCase> sortedTestcases = testcaseDurations.Keys.OrderByDescending(TC => testcaseDurations[TC]).ToList();
            int nrOfThreadsToUse = Options.MaxNrOfThreads;
            int targetDuration = overallDuration / nrOfThreadsToUse;

            List<List<TestCase>> splitTestcases = new List<List<TestCase>>();
            List<TestCase> currentList = new List<TestCase>();
            int currentDuration = 0;
            while (sortedTestcases.Count > 0 && splitTestcases.Count < nrOfThreadsToUse)
            {
                do
                {
                    TestCase testcase = sortedTestcases[0];

                    sortedTestcases.RemoveAt(0);
                    currentList.Add(testcase);
                    currentDuration += testcaseDurations[testcase];
                } while (sortedTestcases.Count > 0 && currentDuration + testcaseDurations[sortedTestcases[0]] <= targetDuration);
                       
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
                int duration = testcases.Sum(TC => testcaseDurations[TC]);
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