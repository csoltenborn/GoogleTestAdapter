using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Model;
using GoogleTestAdapterVSIX.TestFrameworkIntegration;
using GoogleTestAdapterVSIX.TestFrameworkIntegration.Helpers;

namespace GoogleTestAdapter
{
    public abstract class AbstractGoogleTestExecutorTests : AbstractGoogleTestExtensionTests
    {

        private bool ParallelTestExecution { get; }
        private int MaxNrOfThreads { get; }


        protected AbstractGoogleTestExecutorTests(bool parallelTestExecution, int maxNrOfThreads)
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
        public virtual void CheckThatTestDirectoryIsPassedViaCommandLineArg()
        {
            Model.TestCase testCase = GetTestCasesOfConsoleApplication1("CommandArgs.TestDirectoryIsSet").First();

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

            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed)),
                Times.Exactly(1));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>(), It.Is<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome>(to => to == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed)),
                Times.Exactly(0));
        }

        [TestMethod]
        public virtual void RunsExternallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunsExternallyLinkedX86TestsWithResultInDebugMode()
        {
            // for at least having the debug messaging code executed once
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunsStaticallyLinkedX86TestsWithResult()
        {
            // let's print the test output
            MockOptions.Setup(o => o.PrintTestOutput).Returns(true);

            RunAndVerifyTests(X86StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public virtual void RunsExternallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunsStaticallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(X64StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public virtual void RunsCrashingX64TestsWithoutResult()
        {
            RunAndVerifyTests(X64CrashingTests, 0, 2, 0);
        }

        [TestMethod]
        public virtual void RunsCrashingX86TestsWithoutResult()
        {
            RunAndVerifyTests(X86CrashingTests, 0, 2, 0);
        }

        [TestMethod]
        public virtual void RunsHardCrashingX86TestsWithoutResult()
        {
            TestExecutor executor = new TestExecutor(TestEnvironment);
            executor.RunTests(HardCrashingSampleTests.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(0, 1, 0, 3);
        }

        [TestMethod]
        public virtual void RunsWithSetupAndTeardownBatches_TeardownFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(Results0Batch);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(Results1Batch);

            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_SETUP))),
                Times.Never);
            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_TEARDOWN))),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public virtual void RunsWithSetupAndTeardownBatches_SetupFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(Results1Batch);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(Results0Batch);

            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_SETUP))),
                Times.AtLeastOnce());
            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_TEARDOWN))),
                Times.Never);
        }

        [TestMethod]
        public virtual void RunsWithoutBatches_NoLogging()
        {
            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.SendMessage(
                It.IsAny<TestMessageLevel>(),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_SETUP))),
                Times.Never);
            MockLogger.Verify(l => l.SendMessage(
                It.IsAny<TestMessageLevel>(),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_TEARDOWN))),
                Times.Never);
        }

        [TestMethod]
        public virtual void RunsWithNonexistingSetupBatch_LogsError()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns("some_nonexisting_file");

            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains(PreparingTestRunner.TEST_SETUP.ToLower()))),
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