using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.VS
{
    [TestClass]
    public class TestExecutorSequentialTests : AbstractTestExecutorTests
    {

        public TestExecutorSequentialTests() : base(false, 1) { }

        override protected void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests)
        {
            base.CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfNotFoundTests);

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped)),
                Times.Exactly(nrOfNotFoundTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Skipped)),
                Times.Exactly(nrOfNotFoundTests));
        }


        [TestMethod]
        public void CancelingExecutorStopsTestExecution()
        {
            List<Model.TestCase> testCasesToRun = GetTestCasesOfSampleTests("Crashing.LongRunning", "LongRunningTests.Test3");

            Stopwatch stopwatch = new Stopwatch();
            TestExecutor executor = new TestExecutor(TestEnvironment);
            Thread thread = new Thread(() => executor.RunTests(testCasesToRun.Select(DataConversionExtensions.ToVsTestCase), MockRunContext.Object, MockFrameworkHandle.Object));

            stopwatch.Start();
            thread.Start();
            Thread.Sleep(1000);
            executor.Cancel();
            thread.Join();
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 2000); // 1st test should be executed
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000); // 2nd test should not be executed 
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