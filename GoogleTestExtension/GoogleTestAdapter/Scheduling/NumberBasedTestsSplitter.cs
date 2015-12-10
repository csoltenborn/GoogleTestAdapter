using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Scheduling
{
    public class NumberBasedTestsSplitter : ITestsSplitter
    {
        private IEnumerable<TestCase2> TestcasesToRun { get; }
        private TestEnvironment TestEnvironment { get; }


        public NumberBasedTestsSplitter(IEnumerable<TestCase2> testcasesToRun, TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
            this.TestcasesToRun = testcasesToRun;
        }


        public List<List<TestCase2>> SplitTestcases()
        {
            int nrOfThreadsToUse = Math.Min(TestEnvironment.Options.MaxNrOfThreads, TestcasesToRun.Count());
            List<TestCase2>[] splitTestCases = new List<TestCase2>[nrOfThreadsToUse];
            for (int i = 0; i < nrOfThreadsToUse; i++)
            {
                splitTestCases[i] = new List<TestCase2>();
            }

            int testcaseCounter = 0;
            foreach (TestCase2 testCase in TestcasesToRun)
            {
                splitTestCases[testcaseCounter++ % nrOfThreadsToUse].Add(testCase);
            }

            return splitTestCases.ToList();
        }

    }

}