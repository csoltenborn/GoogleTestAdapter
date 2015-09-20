using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Scheduling
{
    class NumberBasedTestsSplitter : AbstractOptionsProvider, ITestsSplitter
    {
        private IEnumerable<TestCase> TestcasesToRun { get; }

        internal NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun, AbstractOptions options) : base(options)
        {
            this.TestcasesToRun = testcasesToRun;
        }

        List<List<TestCase>> ITestsSplitter.SplitTestcases()
        {
            int nrOfThreadsToUse = Math.Min(Options.MaxNrOfThreads, TestcasesToRun.Count());
            List<TestCase>[] splitTestCases = new List<TestCase>[nrOfThreadsToUse];
            for (int i = 0; i < nrOfThreadsToUse; i++)
            {
                splitTestCases[i] = new List<TestCase>();
            }

            int testcaseCounter = 0;
            foreach (TestCase testCase in TestcasesToRun)
            {
                splitTestCases[testcaseCounter++ % nrOfThreadsToUse].Add(testCase);
            }

            return splitTestCases.ToList();
        }

    }

}