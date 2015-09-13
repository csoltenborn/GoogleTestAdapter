using Moq;
using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace GoogleTestAdapter
{
    [TestClass]
    public class ParallelTestRunnerTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ParallelTestExecutionSpeedsUpTestExecution()
        {
            if (Environment.ProcessorCount < 4)
            {
                Assert.Inconclusive("This test is designed for machines with at least 4 cores");
            }

            string executable = GoogleTestDiscovererTests.x86traitsTests;
            Mock<IFrameworkHandle> MockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> MockRunContext = new Mock<IRunContext>();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            GoogleTestExecutor Executor = new GoogleTestExecutor(MockOptions.Object);
            Executor.RunTests(executable.Yield(), MockRunContext.Object, MockHandle.Object);
            stopwatch.Stop();
            long sequentialDuration = stopwatch.ElapsedMilliseconds;

            MockOptions.Setup(O => O.ParallelTestExecution).Returns(true);
            MockOptions.Setup(O => O.MaxNrOfThreads).Returns(Environment.ProcessorCount);

            stopwatch.Restart();
            Executor = new GoogleTestExecutor(MockOptions.Object);
            Executor.RunTests(executable.Yield(), MockRunContext.Object, MockHandle.Object);
            stopwatch.Stop();
            long parallelDuration = stopwatch.ElapsedMilliseconds;

            Assert.IsTrue(sequentialDuration > 6000); // 3 long tests, 2 seconds per test
            Assert.IsTrue(parallelDuration > 2000); 
            Assert.IsTrue(parallelDuration < 3000); // 2 seconds per long test + some time for the rest
        }

    }

}