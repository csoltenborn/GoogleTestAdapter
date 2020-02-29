// This file has been modified by Microsoft on 6/2017.

using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Helpers;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    public class TestExecutorSequentialTests : TestExecutorTestsBase
    {
        private const int DurationOfEachLongRunningTestInMs = 2000;
        private const int WaitBeforeCancelInMs = 1000;
        private static readonly int OverheadInMs = !CiSupport.IsRunningOnBuildServer ? 500 : 1700;

        public TestExecutorSequentialTests() : base(false, 1) { }

        protected override void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfSkippedTests, int nrOfNotFoundTests)
        {
            base.CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfSkippedTests, nrOfNotFoundTests);

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped)),
                Times.Exactly(nrOfSkippedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Skipped)),
                Times.Exactly(nrOfSkippedTests));

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.NotFound)),
                Times.Exactly(nrOfNotFoundTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.NotFound)),
                Times.Exactly(nrOfNotFoundTests));
        }


        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_CancelingExecutor_StopsTestExecution()
        {
            DoRunCancelingTests(false, DurationOfEachLongRunningTestInMs);  // (only) 1st test should be executed
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_CancelingExecutorAndKillProcesses_StopsTestExecutionFaster()
        {
            DoRunCancelingTests(true, WaitBeforeCancelInMs);  // 1st test should be actively canceled
        }

        private void DoRunCancelingTests(bool killProcesses, int lower)
        {
            MockOptions.Setup(o => o.KillProcessesOnCancel).Returns(killProcesses);
            List<Model.TestCase> testCasesToRun = TestDataCreator.GetTestCases("Crashing.LongRunning", "LongRunningTests.Test2");
            testCasesToRun.Should().HaveCount(2);

            var stopwatch = new Stopwatch();
            var executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);

            var canceller = new Thread(() =>
            {
                Thread.Sleep(WaitBeforeCancelInMs);
                executor.Cancel();
            });
            canceller.Start();

            stopwatch.Start();
            executor.RunTests(testCasesToRun.Select(tc => tc.ToVsTestCase()), MockRunContext.Object, MockFrameworkHandle.Object);
            stopwatch.Stop();

            canceller.Join();
            stopwatch.ElapsedMilliseconds.Should().BeInRange(lower, lower + OverheadInMs);
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
        [TestCategory(Integration)]
        public virtual void RunTests_CrashingX64Tests_CorrectTestResults()
        {
            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX64, 1, 2, 0, 3);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_CrashingX86Tests_CorrectTestResults()
        {
            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX86, 1, 2, 0, 3);
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