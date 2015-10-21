using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class ParallelTestRunnerTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ParallelTestExecutionSpeedsUpTestExecution()
        {
            if (Environment.ProcessorCount < 2)
            {
                Assert.Inconclusive("This test is designed for multi-core machines");
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            GoogleTestExecutor executor = new GoogleTestExecutor(TestEnvironment);
            executor.RunTests(SampleTests.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();
            long sequentialDuration = stopwatch.ElapsedMilliseconds;

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(true);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount);

            stopwatch.Restart();
            executor = new GoogleTestExecutor(TestEnvironment);
            executor.RunTests(SampleTests.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();
            long parallelDuration = stopwatch.ElapsedMilliseconds;

            Assert.IsTrue(sequentialDuration > 4000, sequentialDuration.ToString()); // 2 long tests, 2 seconds per test
            Assert.IsTrue(parallelDuration > 2000, parallelDuration.ToString());
            Assert.IsTrue(parallelDuration < 3000, parallelDuration.ToString()); // 2 seconds per long test + some time for the rest
        }

    }

}