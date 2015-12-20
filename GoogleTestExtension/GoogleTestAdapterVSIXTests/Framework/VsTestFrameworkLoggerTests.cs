using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.VS.Framework
{
    [TestClass]
    public class VsTestFrameworkLoggerTests : AbstractVSTests
    {

        private VsTestFrameworkLogger Logger;

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            Logger = new VsTestFrameworkLogger(MockVsLogger.Object);
        }


        [TestMethod]
        public void LogInfoHandlesNull_ProducesNonNullOrWhitespace()
        {
            Logger.LogInfo(null);

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfoHandlesEmptyString_ProducesNonNullOrWhitespace()
        {
            Logger.LogInfo("");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfoHandlesWhitespace_ProducesNonNullOrWhitespace()
        {
            Logger.LogInfo("\n");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogWarning_ProducesWarningPlusMessage()
        {
            Logger.LogWarning("foo");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("Warning") && s.Contains("foo"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogError_ProducesErrorPlusMessage()
        {
            Logger.LogError("bar");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("ERROR") && s.Contains("bar"))),
                Times.Exactly(1));
        }

    }

}