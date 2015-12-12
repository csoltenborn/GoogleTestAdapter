using GoogleTestAdapterVSIX.TestFrameworkIntegration;
using GoogleTestAdapterVSIX.TestFrameworkIntegration.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
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

            ILogger logger = new VsTestFrameworkLogger(MockLogger.Object);
            Environment = new TestEnvironment(MockOptions.Object, logger);
        }


        [TestMethod]
        public void LogInfoHandlesNull_ProducesNonNullOrWhitespace()
        {
            Environment.LogInfo(null);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfoHandlesEmptyString_ProducesNonNullOrWhitespace()
        {
            Environment.LogInfo("");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfoHandlesWhitespace_ProducesNonNullOrWhitespace()
        {
            Environment.LogInfo("\n");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogWarning_ProducesWarningPlusMessage()
        {
            Environment.LogWarning("foo");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("Warning") && s.Contains("foo"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogError_ProducesErrorPlusMessage()
        {
            Environment.LogError("bar");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("ERROR") && s.Contains("bar"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfoAsDebug_ProducesMessageOnlyIfDebugMode()
        {
            MockOptions.Setup(o => o.DebugMode).Returns(false);

            Environment.DebugInfo("bar");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s.Contains("bar"))),
                Times.Never());

            MockOptions.Setup(o => o.DebugMode).Returns(true);

            Environment.DebugInfo("bar");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s.Contains("bar"))),
                Times.Exactly(1));
        }

    }

}