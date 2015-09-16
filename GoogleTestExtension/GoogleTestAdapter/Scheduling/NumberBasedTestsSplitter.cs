using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Scheduling
{
    public class NumberBasedTestsSplitter : AbstractGoogleTestAdapterClass, ITestsSplitter
    {
        private IEnumerable<TestCase> TestcasesToRun { get; }

        public NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun) : this(testcasesToRun, null) { }

        public NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun, AbstractOptions options) : base(options)
        {
            this.TestcasesToRun = testcasesToRun;
        }

        public List<List<TestCase>> SplitTestcases()
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