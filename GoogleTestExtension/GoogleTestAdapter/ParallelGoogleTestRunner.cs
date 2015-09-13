using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Scheduling;

namespace GoogleTestAdapter
{
    public class ParallelGoogleTestRunner : AbstractGoogleTestAdapterClass, IGoogleTestRunner
    {
        public bool Canceled { get; set; }

        public ParallelGoogleTestRunner(IOptions options) : base(options) { }

        public void RunTests(bool runAllTestCases, IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun, IRunContext runContext, IFrameworkHandle handle)
        {
            TestDurationSerializer serializer = new TestDurationSerializer();
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testCasesToRun);
            ITestsSplitter splitter;
            if (durations.Count < testCasesToRun.Count())
            {
                splitter = new NumberBasedTestsSplitter(testCasesToRun);
            }
            else
            {
                splitter = new DurationBasedTestsSplitter(durations);
            }

            List<List<TestCase>> splittedTestCasesToRun = splitter.SplitTestcases();
            List<Thread> threads = new List<Thread>();
            foreach (List<TestCase> testcases in splittedTestCasesToRun)
            {
                GoogleTestRunner actualRunner = new GoogleTestRunner(Options);
                Thread thread = new Thread(() => actualRunner.RunTests(false, allTestCases, testcases, runContext, handle));
                thread.Start();
                threads.Add(thread);
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

    }

}