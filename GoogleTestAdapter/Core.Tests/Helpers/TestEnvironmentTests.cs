using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class TestEnvironmentTests : AbstractCoreTests
    {
        private TestEnvironment _environment;

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            _environment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void LogWarning_ProducesWarningOnLogger()
        {
            _environment.LogWarning("foo");

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("foo"))), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void LogError_ProducesErrorOnLogger()
        {
            _environment.LogError("bar");

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("bar"))), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebugInfo_InDebugMode_ProducesInfoOnLogger()
        {
            MockOptions.Setup(o => o.DebugMode).Returns(true);
            _environment.DebugInfo("bar");
            MockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("bar"))), Times.Exactly(1));
        }
        [TestMethod]
        [TestCategory(Unit)]
        public void DebugInfo_NotInDebugMode_DoesNotProduceLogging()
        {
            MockOptions.Setup(o => o.DebugMode).Returns(false);
            _environment.DebugInfo("bar");
            MockLogger.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("bar"))), Times.Never());
        }

    }

}