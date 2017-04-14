﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class SequentialTestRunnerTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CancelingDuringTestExecution_StopsTestExecution()
        {
            DoRunCancelingTests(
                false, 
                2000,  // 1st test should be executed
                3000); // 2nd test should not be executed 
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CancelingAndKillingProcessesDuringTestExecution_StopsTestExecutionFaster()
        {
            DoRunCancelingTests(
                true,
                1000,  // 1st test should be canceled
                2000); // 2nd test should not be executed 
        }

        private void DoRunCancelingTests(bool killProcesses, int lower, int upper)
        {
            MockOptions.Setup(o => o.KillProcessesOnCancel).Returns(killProcesses);
            List<TestCase> allTestCases = TestDataCreator.AllTestCasesExceptLoadTests;
            List<TestCase> testCasesToRun = TestDataCreator.GetTestCases("Crashing.LongRunning", "LongRunningTests.Test2");

            var stopwatch = new Stopwatch();
            var runner = new SequentialTestRunner("", null, MockFrameworkReporter.Object, new SchedulingAnalyzer(TestEnvironment.Logger), TestEnvironment.Options, TestEnvironment.Logger);
            var thread = new Thread(() => runner.RunTests(allTestCases, testCasesToRun, "", "", ""));

            stopwatch.Start();
            thread.Start();
            Thread.Sleep(1000);
            runner.Cancel();
            thread.Join();
            stopwatch.Stop();

            testCasesToRun.Count.Should().Be(2);
            MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);

            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(lower); // 1st test should be executed
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(upper); // 2nd test should not be executed 
        }

    }

}