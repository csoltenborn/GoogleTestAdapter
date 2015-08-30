using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestExecutorTests : AbstractGoogleTestExtensionTests
    {
        class MockedGoogleTestExecutor : GoogleTestExecutor
        {
            private readonly Mock<IOptions> MockedOptions;

            internal MockedGoogleTestExecutor(Mock<IOptions> mockedOptions)
            {
                this.MockedOptions = mockedOptions;
            }

            protected override IOptions Options => MockedOptions.Object;
        }

        [TestMethod]
        public void RunsExternallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x86externallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public void RunsStaticallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x86staticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public void RunsExternallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x64externallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public void RunsStaticallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x64staticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public void RunsCrashingX64TestsWithoutResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x64crashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public void RunsCrashingX86TestsWithoutResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x86crashingTests, 0, 1, 0, 1);
        }

        private void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests = 0)
        {
            Mock<IFrameworkHandle> MockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> MockRunContext = new Mock<IRunContext>();

            GoogleTestExecutor Executor = new MockedGoogleTestExecutor(MockOptions);
            Executor.RunTests(executable.Yield(), MockRunContext.Object, MockHandle.Object);

            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.NotFound)),
                Times.Exactly(nrOfNotFoundTests));
        }

    }

}