using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Scheduling
{
    public class NumberBasedTestsSplitter : ITestsSplitter
    {
        private readonly IEnumerable<TestCase> _testcasesToRun;
        private readonly SettingsWrapper _settings;


        public NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun, SettingsWrapper settings)
        {
            _settings = settings;
            _testcasesToRun = testcasesToRun;
        }


        public List<List<TestCase>> SplitTestcases()
        {
            int nrOfThreadsToUse = Math.Min(_settings.MaxNrOfThreads, _testcasesToRun.Count());
            var splitTestCases = new List<TestCase>[nrOfThreadsToUse];
            for (int i = 0; i < nrOfThreadsToUse; i++)
            {
                splitTestCases[i] = new List<TestCase>();
            }

            int testcaseCounter = 0;
            foreach (TestCase testCase in _testcasesToRun)
            {
                splitTestCases[testcaseCounter++ % nrOfThreadsToUse].Add(testCase);
            }

            return splitTestCases.ToList();
        }

    }

}