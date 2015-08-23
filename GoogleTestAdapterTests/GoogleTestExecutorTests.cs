using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestExecutorTests : AbstractGoogleTestExtensionTests
    {
        private readonly Mock<IRunContext> runContext = new Mock<IRunContext>();
        private GoogleTestExecutor executor;

        [TestInitialize]
        public void SetUp()
        {
            executor = new GoogleTestExecutor();
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
            Mock<IFrameworkHandle> handle = new Mock<IFrameworkHandle>();

            executor.RunTests(executable.Yield(), runContext.Object, handle.Object);

            handle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));
            handle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));
            handle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            handle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.NotFound)),
                Times.Exactly(nrOfNotFoundTests));
        }

    }

}