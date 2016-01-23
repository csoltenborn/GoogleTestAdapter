using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class TestEnvironmentTests : AbstractGoogleTestExtensionTests
    {
        private TestEnvironment Environment;

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            Environment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
        }


        [TestMethod]
        public void LogWarning_ProducesWarningOnLogger()
        {
            Environment.LogWarning("foo");

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("foo"))), Times.Exactly(1));
        }

        [TestMethod]
        public void LogError_ProducesErrorOnLogger()
        {
            Environment.LogError("bar");

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("bar"))), Times.Exactly(1));
        }

        [TestMethod]
        public void DebugInfo_InDebugMode_ProducesInfoOnLogger()
        {
            MockOptions.Setup(o => o.DebugMode).Returns(true);
            Environment.DebugInfo("bar");
            MockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("bar"))), Times.Exactly(1));
        }
        [TestMethod]
        public void DebugInfo_NotInDebugMode_DoesNotProduceLogging()
        {
            MockOptions.Setup(o => o.DebugMode).Returns(false);
            Environment.DebugInfo("bar");
            MockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("bar"))), Times.Never());
        }

    }

}