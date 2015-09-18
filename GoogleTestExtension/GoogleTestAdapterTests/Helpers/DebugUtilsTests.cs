using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Reflection;
using Moq;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class DebugUtilsTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void LogDebugMessageLogsInDebugMode()
        {
            FieldInfo fieldInfo = typeof(DebugUtils).GetField("DebugMode", BindingFlags.NonPublic | BindingFlags.Static);
            fieldInfo.SetValue(null, true);

            DebugUtils.LogDebugMessage(MockLogger.Object, TestMessageLevel.Informational, "my msg");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => s == "my msg")),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogDebugMessageDoesNotLogInDebugMode()
        {
            FieldInfo fieldInfo = typeof(DebugUtils).GetField("DebugMode", BindingFlags.NonPublic | BindingFlags.Static);
            fieldInfo.SetValue(null, false);

            DebugUtils.LogDebugMessage(MockLogger.Object, TestMessageLevel.Informational, "my msg");

            MockLogger.Verify(l => l.SendMessage(
                It.IsAny<TestMessageLevel>(),
                It.IsAny<string>()),
                Times.Exactly(0));
        }

        [TestMethod]
        public void LogUserDebugMessageLogsInUserDebugMode()
        {
            MockOptions.Setup(o => o.UserDebugMode).Returns(true);
            DebugUtils.LogUserDebugMessage(MockLogger.Object, MockOptions.Object, TestMessageLevel.Warning, "my msg");

            MockLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s == "my msg")),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogUserDebugMessageDoesNotLogInUserDebugMode()
        {
            MockOptions.Setup(o => o.UserDebugMode).Returns(false);
            DebugUtils.LogUserDebugMessage(MockLogger.Object, MockOptions.Object, TestMessageLevel.Warning, "my msg");

            MockLogger.Verify(l => l.SendMessage(
                It.IsAny<TestMessageLevel>(),
                It.IsAny<string>()),
                Times.Exactly(0));
        }

    }

}