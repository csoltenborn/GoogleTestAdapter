using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    public abstract class AbstractGoogleTestExecutorTests : AbstractGoogleTestExtensionTests
    {

        protected abstract bool ParallelTestExecution { get; }
        protected abstract int MaxNrOfThreads { get; }

        protected virtual void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests)
        {
            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.None)),
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
            TestCase testCase = GetTestCasesOfConsoleApplication1("CommandArgs.TestDirectoryIsSet").First();

            GoogleTestExecutor executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(testCase.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(0));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(1));

            MockFrameworkHandle.Reset();
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-testdirectory=\"" + GoogleTestAdapterOptions.TestDirPlaceholder + "\"");

            executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(testCase.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(1));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(0));
        }

        [TestMethod]
        public virtual void RunsExternallyLinkedX86TestsWithResult()
        {
            // also tests batch execution
            MockOptions.Setup(o => o.TestSetupBatch).Returns(Results0Batch);
            MockOptions.Setup(o => o.TestTeardownBatch).Returns(Results1Batch);

            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);

            MockFrameworkHandle.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("setup"))),
                Times.Never);
            MockFrameworkHandle.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("teardown"))),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public virtual void RunsExternallyLinkedX86TestsWithResultInDebugMode()
        {
            // for at least having the debug messaging code executed once
            FieldInfo fieldInfo = typeof(DebugUtils).GetField("DebugMode", BindingFlags.NonPublic | BindingFlags.Static);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(null, true);
            MockOptions.Setup(o => o.UserDebugMode).Returns(true);

            RunAndVerifyTests(X86ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public virtual void RunsStaticallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(X86StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public virtual void RunsExternallyLinkedX64TestsWithResult()
        {
            // also tests batch execution
            MockOptions.Setup(o => o.TestSetupBatch).Returns(Results1Batch);
            MockOptions.Setup(o => o.TestTeardownBatch).Returns(Results0Batch);

            RunAndVerifyTests(X64ExternallyLinkedTests, 2, 0, 0);

            MockFrameworkHandle.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("setup"))),
                Times.AtLeastOnce());
            MockFrameworkHandle.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("teardown"))),
                Times.Never);
        }

        [TestMethod]
        public virtual void RunsStaticallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(X64StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public virtual void RunsCrashingX64TestsWithoutResult()
        {
            RunAndVerifyTests(X64CrashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public virtual void RunsCrashingX86TestsWithoutResult()
        {
            RunAndVerifyTests(X86CrashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public virtual void RunsHardCrashingX86TestsWithoutResult()
        {
            GoogleTestExecutor executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(X86HardcrashingTests.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(0, 1, 0, 3);
        }

        private void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests = 0)
        {
            GoogleTestExecutor executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(executable.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfNotFoundTests);
        }

    }

}