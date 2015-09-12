using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;

namespace GoogleTestAdapter.Scheduling
{
    public class NumberBasedTestsSplitter : AbstractGoogleTestAdapterClass, ITestsSplitter
    {
        private readonly IEnumerable<TestCase> testcasesToRun;

        public NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun) : this(testcasesToRun, null) { }

        public NumberBasedTestsSplitter(IEnumerable<TestCase> testcasesToRun, IOptions options) : base(options)
        {
            this.testcasesToRun = testcasesToRun;
        }

        public List<List<TestCase>> SplitTestcases()
        {
            int nrOfThreadsToUse = Math.Min(Options.MaxNrOfThreads, testcasesToRun.Count());
            List<TestCase>[] splitTestCases = new List<TestCase>[nrOfThreadsToUse];
            for (int i = 0; i < nrOfThreadsToUse; i++)
            {
                splitTestCases[i] = new List<TestCase>();
            }

            int testcaseCounter = 0;
            foreach (TestCase testCase in testcasesToRun)
            {
                splitTestCases[testcaseCounter++ % nrOfThreadsToUse].Add(testCase);
            }

            return splitTestCases.ToList();
        }

    }

}