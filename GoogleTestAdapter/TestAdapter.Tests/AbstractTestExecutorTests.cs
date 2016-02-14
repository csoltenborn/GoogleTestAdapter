using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestAdapter
{
    public abstract class AbstractTestExecutorTests : AbstractVSTests
    {

        private bool ParallelTestExecution { get; }
        private int MaxNrOfThreads { get; }


        protected AbstractTestExecutorTests(bool parallelTestExecution, int maxNrOfThreads)
        {
            this.ParallelTestExecution = parallelTestExecution;
            this.MaxNrOfThreads = maxNrOfThreads;
        }


        protected virtual void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests)
        {
            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult>(tr => tr.Outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
        }

        [TestInitialize]
        override public void SetUp()
        {
            base.SetUp();

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(ParallelTestExecution);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(MaxNrOfThreads);
        }


        [TestMethod]
        public virtual void RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg()
        {
            TestCase testCase = GetTestCasesOfSampleTests("CommandArgs.TestDirectoryIsSet").First();

            TestExecutor executor = new TestExecutor(TestEnvironment);
            executor.RunTests(DataConversionExtensions.ToVsTestCase(testCase).Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed)),
                Times.Exactly(0));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed)),
                Times.Exactly(1));

            MockFrameworkHandle.Reset();
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-testdirectory=\"" + Options.TestDirPlaceholder + "\"");

            executor = new TestExecutor(TestEnvironment);
            executor.RunTests(DataConversionExtensions.ToVsTestCase(testCase).Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed)),
                Times.Exactly(0));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed)),
                Times.Exactly(1));
        }

        [TestMethod]
        public virtual void RunTests_ExternallyLinkedX86Tests_CorrectTestResults()
        {
            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults()
        {
            // for at least having the debug messaging code executed once
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunTests_StaticallyLinkedX86Tests_CorrectTestResults()
        {
            // let's print the test output
            MockOptions.Setup(o => o.PrintTestOutput).Returns(true);

            RunAndVerifyTests(X86StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public virtual void RunTests_ExternallyLinkedX64_CorrectTestResults()
        {
            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunTests_StaticallyLinkedX64Tests_CorrectTestResults()
        {
            RunAndVerifyTests(X64StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public virtual void RunTests_CrashingX64Tests_CorrectTestResults()
        {
            RunAndVerifyTests(X64CrashingTests, 0, 2, 0);
        }

        [TestMethod]
        public virtual void RunTests_CrashingX86Tests_CorrectTestResults()
        {
            RunAndVerifyTests(X86CrashingTests, 0, 2, 0);
        }

        [TestMethod]
        public virtual void RunTests_HardCrashingX86Tests_CorrectTestResults()
        {
            TestExecutor executor = new TestExecutor(TestEnvironment);
            executor.RunTests(HardCrashingSampleTests.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(1, 2, 0, 3);
        }

        [TestMethod]
        public virtual void RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(Results0Batch);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(Results1Batch);

            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public virtual void RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(Results1Batch);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(Results0Batch);

            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.AtLeastOnce());
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
        }

        [TestMethod]
        public virtual void RunTests_WithoutBatches_NoLogging()
        {
            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.LogInfo(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogInfo(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
        }

        [TestMethod]
        public virtual void RunTests_WithNonexistingSetupBatch_LogsError()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns("some_nonexisting_file");

            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup.ToLower()))),
                Times.AtLeastOnce());
        }


        private void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests = 0)
        {
            TestExecutor executor = new TestExecutor(TestEnvironment);
            executor.RunTests(executable.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfNotFoundTests);
        }

    }

}