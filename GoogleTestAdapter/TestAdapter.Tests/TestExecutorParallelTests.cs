using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    public class TestExecutorParallelTests : TestExecutorTestsBase
    {

        public TestExecutorParallelTests() : base(true, Environment.ProcessorCount) { }


        protected override void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfSkippedTests, int nrOfNotFoundTests)
        {
            base.CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfSkippedTests, nrOfNotFoundTests);

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

            if (nrOfNotFoundTests > 0)
            {
                MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.NotFound)),
                    Times.AtLeast(nrOfNotFoundTests));
                MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.NotFound)),
                    Times.AtLeast(nrOfNotFoundTests));
            }

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped)),
                Times.AtMost(nrOfSkippedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Skipped)),
                Times.AtMost(nrOfSkippedTests));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_ParallelTestExecution_SpeedsUpTestExecution()
        {
            MockOptions.Setup(o => o.ParallelTestExecution).Returns(false);

            Stopwatch stopwatch = new Stopwatch();
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);
            IEnumerable<string> testsToRun = TestResources.LongRunningTests_ReleaseX86.Yield();
            stopwatch.Start();
            executor.RunTests(testsToRun, MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();
            long sequentialDuration = stopwatch.ElapsedMilliseconds;

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(true);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount);

            executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);
            testsToRun = TestResources.LongRunningTests_ReleaseX86.Yield();
            stopwatch.Restart();
            executor.RunTests(testsToRun, MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();
            long parallelDuration = stopwatch.ElapsedMilliseconds;

            sequentialDuration.Should().BeGreaterThan(4000); // 2 long tests, 2 seconds per test
            parallelDuration.Should().BeGreaterThan(2000);
            parallelDuration.Should().BeLessThan(4000); // 2 seconds per long test + some time for the rest
        }


        #region Method stubs for code coverage

        [TestMethod]
        public override void RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg()
        {
            base.RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg();
        }

        [TestMethod]
        public override void RunTests_WorkingDir_IsSetCorrectly()
        {
            base.RunTests_WorkingDir_IsSetCorrectly();
        }

        [TestMethod]
        public override void RunTests_ExternallyLinkedX86Tests_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX86Tests_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_StaticallyLinkedX86Tests_CorrectTestResults()
        {
            base.RunTests_StaticallyLinkedX86Tests_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_ExternallyLinkedX64_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX64_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_StaticallyLinkedX64Tests_CorrectTestResults()
        {
            base.RunTests_StaticallyLinkedX64Tests_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_StaticallyLinkedX64Tests_OutputIsPrintedAtMostOnce()
        {
            base.RunTests_StaticallyLinkedX64Tests_OutputIsPrintedAtMostOnce();
        }

        [TestMethod]
        public override void RunTests_HardCrashingX86Tests_CorrectTestResults()
        {
            base.RunTests_HardCrashingX86Tests_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_WithoutBatches_NoLogging()
        {
            base.RunTests_WithoutBatches_NoLogging();
        }

        [TestMethod]
        public override void RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning()
        {
            base.RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning();
        }

        [TestMethod]
        public override void RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning()
        {
            base.RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning();
        }

        [TestMethod]
        public override void RunTests_WithNonexistingSetupBatch_LogsError()
        {
            base.RunTests_WithNonexistingSetupBatch_LogsError();
        }

        [TestMethod]
        public override void RunTests_WithPathExtension_ExecutionOk()
        {
            base.RunTests_WithPathExtension_ExecutionOk();
        }

        [TestMethod]
        public override void RunTests_WithoutPathExtension_ExecutionFails()
        {
            base.RunTests_WithoutPathExtension_ExecutionFails();
        }

        [TestMethod]
        public override void RunTests_ExitCodeTest_PassingTestResultIsProduced()
        {
            base.RunTests_ExitCodeTest_PassingTestResultIsProduced();
        }

        [TestMethod]
        public override void RunTests_ExitCodeTest_FailingTestResultIsProduced()
        {
            base.RunTests_ExitCodeTest_FailingTestResultIsProduced();
        }

        [TestMethod]
        public override void MemoryLeakTests_FailingWithLeaks_CorrectResult()
        {
            base.MemoryLeakTests_FailingWithLeaks_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_PassingWithLeaks_CorrectResult()
        {
            base.MemoryLeakTests_PassingWithLeaks_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_PassingWithoutLeaksRelease_CorrectResult()
        {
            base.MemoryLeakTests_PassingWithoutLeaksRelease_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_PassingWithoutLeaks_CorrectResult()
        {
            base.MemoryLeakTests_PassingWithoutLeaks_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_FailingWithoutLeaks_CorrectResult()
        {
            base.MemoryLeakTests_FailingWithoutLeaks_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_ExitCodeTest_OnlyexitCodeTestResultAndNoWarnings()
        {
            base.MemoryLeakTests_ExitCodeTest_OnlyexitCodeTestResultAndNoWarnings();
        }

        #endregion

    }

}