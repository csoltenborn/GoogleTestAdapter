using System.Reflection;
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

            Environment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
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
            FieldInfo fieldInfo = typeof(TestEnvironment).GetField("DebugMode", BindingFlags.NonPublic | BindingFlags.Static);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(null, false);

            Environment.LogInfo("bar", TestEnvironment.LogType.Debug);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s.Contains("bar"))),
                Times.Never());

            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(null, true);

            Environment.LogInfo("bar", TestEnvironment.LogType.Debug);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s.Contains("bar"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfoAsUserDebug_ProducesMessageOnlyIfUserDebugMode()
        {
            Environment.LogInfo("bar", TestEnvironment.LogType.UserDebug);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s.Contains("bar"))),
                Times.Never());

            MockOptions.Setup(o => o.UserDebugMode).Returns(true);

            Environment.LogInfo("bar", TestEnvironment.LogType.UserDebug);

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s.Contains("bar"))),
                Times.Exactly(1));
        }

    }

}