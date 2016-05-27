using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Scheduling
{
    public class NumberBasedTestsSplitter : ITestsSplitter
    {
        private readonly IEnumerable<TestCase> _testcasesToRun;
        private readonly TestEnvironment _testEnvironment;


        public NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun, TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
            _testcasesToRun = testcasesToRun;
        }


        public List<List<TestCase>> SplitTestcases()
        {
            int nrOfThreadsToUse = Math.Min(_testEnvironment.Options.MaxNrOfThreads, _testcasesToRun.Count());
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