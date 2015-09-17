using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{
    public abstract class AbstractGoogleTestExecutorTests : AbstractGoogleTestExtensionTests
    {

        protected abstract bool ParallelTestExecution { get; }
        protected abstract int MaxNrOfThreads { get; }

        protected virtual void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests, Mock<IFrameworkHandle> mockHandle)
        {
            mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
        }

        class CollectingTestDiscoverySink : ITestCaseDiscoverySink
        {
            public List<TestCase> TestCases { get; } = new List<TestCase>();

            public void SendTestCase(TestCase discoveredTest)
            {
                TestCases.Add(discoveredTest);
            }
        }

        [TestInitialize]
        override public void SetUp()
        {
            base.SetUp();

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(ParallelTestExecution);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(MaxNrOfThreads);
        }

        [TestMethod]
        public void CheckThatTestDirectoryIsPassedViaCommandLineArg()
        {
            Mock<IFrameworkHandle> mockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> mockRunContext = new Mock<IRunContext>();
            Mock<IDiscoveryContext> mockDiscoveryContext = new Mock<IDiscoveryContext>();
            CollectingTestDiscoverySink sink = new CollectingTestDiscoverySink();

            GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
            discoverer.DiscoverTests(GoogleTestDiscovererTests.X86TraitsTests.Yield(), mockDiscoveryContext.Object, mockHandle.Object, sink);

            TestCase testcase = sink.TestCases.FirstOrDefault(tc => tc.FullyQualifiedName.Contains("CommandArgs.TestDirectoryIsSet"));
            Assert.IsNotNull(testcase);

            GoogleTestExecutor executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(testcase.Yield(), mockRunContext.Object, mockHandle.Object);

            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(0));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(1));

            mockHandle.Reset();
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-testdirectory=\"" + GoogleTestAdapterOptions.TestDirPlaceholder + "\"");

            executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(testcase.Yield(), mockRunContext.Object, mockHandle.Object);

            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(1));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(0));
        }

        [TestMethod]
        public void RunsExternallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.X86ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public void RunsStaticallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.X86StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public void RunsExternallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.X64ExternallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public void RunsStaticallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.X64StaticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public void RunsCrashingX64TestsWithoutResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.X64CrashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public void RunsCrashingX86TestsWithoutResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.X86CrashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public void RunsHardCrashingX86TestsWithoutResult()
        {
            Mock<IFrameworkHandle> mockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> mockRunContext = new Mock<IRunContext>();

            GoogleTestExecutor executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(GoogleTestDiscovererTests.X86HardcrashingTests.Yield(), mockRunContext.Object, mockHandle.Object);

            mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(0));
            mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed && tr.ErrorMessage == "!! This is probably the test that crashed !!")),
                Times.Exactly(1));
            mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(0));
            mockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped && tr.ErrorMessage == "reason is probably a crash of test Crashing.TheCrash")),
                Times.Exactly(2));

            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Passed)),
                Times.Exactly(0));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Failed)),
                Times.Exactly(1));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.None)),
                Times.Exactly(0));
            mockHandle.Verify(h => h.RecordEnd(It.IsAny<TestCase>(), It.Is<TestOutcome>(to => to == TestOutcome.Skipped)),
                Times.Exactly(2));
        }

        private void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests = 0)
        {
            Mock<IFrameworkHandle> mockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> mockRunContext = new Mock<IRunContext>();

            GoogleTestExecutor executor = new GoogleTestExecutor(MockOptions.Object);
            executor.RunTests(executable.Yield(), mockRunContext.Object, mockHandle.Object);

            CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfNotFoundTests, mockHandle);
        }

    }

}