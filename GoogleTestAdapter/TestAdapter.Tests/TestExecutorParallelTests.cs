using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    public class TestExecutorParallelTests : AbstractTestExecutorTests
    {

        public TestExecutorParallelTests() : base(true, Environment.ProcessorCount) { }


        override protected void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests)
        {
            base.CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfNotFoundTests);

            if (nrOfPassedTests > 0)
            {
                MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                    Times.AtLeast(nrOfPassedTests));
                MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                    Times.AtLeast(nrOfPassedTests));
            }

            if (nrOfFailedTests > 0)
            {
                MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                    Times.AtLeast(nrOfFailedTests));
                MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                    Times.AtLeast(nrOfFailedTests));
            }

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped)),
                Times.AtMost(nrOfNotFoundTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Skipped)),
                Times.AtMost(nrOfNotFoundTests));
        }

        [TestMethod]
        public void ParallelTestExecutionSpeedsUpTestExecution()
        {
            MockOptions.Setup(o => o.ParallelTestExecution).Returns(false);

            Stopwatch stopwatch = new Stopwatch();
            TestExecutor executor = new TestExecutor(TestEnvironment);
            IEnumerable<string> testsToRun = SampleTests.Yield();
            stopwatch.Start();
            executor.RunTests(testsToRun, MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();
            long sequentialDuration = stopwatch.ElapsedMilliseconds;

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(true);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount);

            executor = new TestExecutor(TestEnvironment);
            testsToRun = SampleTests.Yield();
            stopwatch.Restart();
            executor.RunTests(testsToRun, MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();
            long parallelDuration = stopwatch.ElapsedMilliseconds;

            Assert.IsTrue(sequentialDuration > 4000, sequentialDuration.ToString()); // 2 long tests, 2 seconds per test
            Assert.IsTrue(parallelDuration > 2000, parallelDuration.ToString());
            Assert.IsTrue(parallelDuration < 3500, parallelDuration.ToString()); // 2 seconds per long test + some time for the rest
        }


        #region Method stubs for code coverage

        [TestMethod]
        public override void CheckThatTestDirectoryIsPassedViaCommandLineArg()
        {
            base.CheckThatTestDirectoryIsPassedViaCommandLineArg();
        }

        [TestMethod]
        public override void RunsExternallyLinkedX86TestsWithResult()
        {
            base.RunsExternallyLinkedX86TestsWithResult();
        }

        [TestMethod]
        public override void RunsStaticallyLinkedX86TestsWithResult()
        {
            base.RunsStaticallyLinkedX86TestsWithResult();
        }

        [TestMethod]
        public override void RunsExternallyLinkedX64TestsWithResult()
        {
            base.RunsExternallyLinkedX64TestsWithResult();
        }

        [TestMethod]
        public override void RunsStaticallyLinkedX64TestsWithResult()
        {
            base.RunsStaticallyLinkedX64TestsWithResult();
        }

        [TestMethod]
        public override void RunsCrashingX64TestsWithoutResult()
        {
            base.RunsCrashingX64TestsWithoutResult();
        }

        [TestMethod]
        public override void RunsCrashingX86TestsWithoutResult()
        {
            base.RunsCrashingX86TestsWithoutResult();
        }

        [TestMethod]
        public override void RunsHardCrashingX86TestsWithoutResult()
        {
            base.RunsHardCrashingX86TestsWithoutResult();
        }

        [TestMethod]
        public override void RunsExternallyLinkedX86TestsWithResultInDebugMode()
        {
            base.RunsExternallyLinkedX86TestsWithResultInDebugMode();
        }

        [TestMethod]
        public override void RunsWithoutBatches_NoLogging()
        {
            base.RunsWithoutBatches_NoLogging();
        }

        [TestMethod]
        public override void RunsWithSetupAndTeardownBatches_SetupFails_LogsWarning()
        {
            base.RunsWithSetupAndTeardownBatches_SetupFails_LogsWarning();
        }

        [TestMethod]
        public override void RunsWithSetupAndTeardownBatches_TeardownFails_LogsWarning()
        {
            base.RunsWithSetupAndTeardownBatches_TeardownFails_LogsWarning();
        }

        [TestMethod]
        public override void RunsWithNonexistingSetupBatch_LogsError()
        {
            base.RunsWithNonexistingSetupBatch_LogsError();
        }

        #endregion

    }

}