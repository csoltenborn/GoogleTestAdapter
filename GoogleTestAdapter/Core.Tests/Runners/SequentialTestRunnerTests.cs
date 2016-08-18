using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class SequentialTestRunnerTests : AbstractCoreTests
    {

        [TestMethod, Ignore]
        [TestCategory(Integration)]
        public void RunTests_CancelingDuringTestExecution_StopsTestExecution()
        {
            List<TestCase> allTestCases = TestDataCreator.AllTestCasesOfSampleTests;
            List<TestCase> testCasesToRun = TestDataCreator.GetTestCasesOfSampleTests("Crashing.LongRunning", "LongRunningTests.Test3");

            var stopwatch = new Stopwatch();
            var runner = new SequentialTestRunner(MockFrameworkReporter.Object, TestEnvironment);
            var executor = new ProcessExecutor(null);
            var thread = new Thread(() => runner.RunTests(allTestCases, testCasesToRun, "", "", "", false, null, executor));

            stopwatch.Start();
            thread.Start();
            Thread.Sleep(1000);
            runner.Cancel();
            thread.Join();
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(2000); // 1st test should be executed
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // 2nd test should not be executed 
        }

    }

}