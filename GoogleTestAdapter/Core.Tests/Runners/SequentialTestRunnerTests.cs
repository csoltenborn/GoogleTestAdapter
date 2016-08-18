using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class SequentialTestRunnerTests : AbstractCoreTests
    {

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CancelingDuringTestExecution_StopsTestExecution()
        {
            List<TestCase> allTestCases = TestDataCreator.AllTestCasesOfSampleTests;
            List<TestCase> testCasesToRun = TestDataCreator.GetTestCasesOfSampleTests("Crashing.LongRunning", "LongRunningTests.Test2");

            var stopwatch = new Stopwatch();
            var runner = new SequentialTestRunner(MockFrameworkReporter.Object, TestEnvironment);
            var executor = new ProcessExecutor(null, MockLogger.Object);
            var thread = new Thread(() => runner.RunTests(allTestCases, testCasesToRun, "", "", "", false, null, executor));

            stopwatch.Start();
            thread.Start();
            Thread.Sleep(1000);
            runner.Cancel();
            thread.Join();
            stopwatch.Stop();

            testCasesToRun.Count.Should().Be(2);
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);

            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(2000); // 1st test should be executed
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // 2nd test should not be executed 
        }

    }

}