using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter
{
    [TestClass]
    public class ParallelGoogleTestExecutorTests : AbstractGoogleTestExecutorTests
    {

        override protected bool ParallelTestExecution => true;

        override protected int MaxNrOfThreads => Environment.ProcessorCount;

        override protected void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests, Mock<IFrameworkHandle> mockHandle)
        {
            base.CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfNotFoundTests, mockHandle);

            if (nrOfPassedTests > 0)
            {
                mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                    Times.AtLeast(nrOfPassedTests));
                mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                    Times.AtLeast(nrOfPassedTests));
            }

            if (nrOfFailedTests > 0)
            {
                mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                    Times.AtLeast(nrOfFailedTests));
                mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                    Times.AtLeast(nrOfFailedTests));
            }

            mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped)),
                Times.AtMost(nrOfNotFoundTests));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Skipped)),
                Times.AtMost(nrOfNotFoundTests));
        }


        // for now, to get test coverage
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
        public override void CancelingSetsCanceledProperty()
        {
            base.CancelingSetsCanceledProperty();
        }

        [TestMethod]
        public override void RunsExternallyLinkedX86TestsWithResultInDebugMode()
        {
            base.RunsExternallyLinkedX86TestsWithResultInDebugMode();
        }

    }

}