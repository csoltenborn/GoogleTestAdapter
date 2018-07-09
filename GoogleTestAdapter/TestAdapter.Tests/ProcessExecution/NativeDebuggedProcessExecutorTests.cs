using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestAdapter.ProcessExecution
{
    [TestClass]
    public class NativeDebuggedProcessExecutorTests : ProcessExecutorTests
    {
        private readonly Mock<IDebuggerAttacher> _mockDebuggerAttacher = new Mock<IDebuggerAttacher>();

        [TestInitialize]
        public void Setup()
        {
            _mockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>())).Returns(true);
            ProcessExecutor = new NativeDebuggedProcessExecutor(_mockDebuggerAttacher.Object, true, MockLogger.Object);
        }

        [TestCleanup]
        public override void Teardown()
        {
            base.Teardown();
            _mockDebuggerAttacher.Reset();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_PingLocalHost()
        {
            Test_ExecuteProcessBlocking_PingLocalHost();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SampleTests()
        {
            Test_ExecuteProcessBlocking_SampleTests();
        }

    }

}