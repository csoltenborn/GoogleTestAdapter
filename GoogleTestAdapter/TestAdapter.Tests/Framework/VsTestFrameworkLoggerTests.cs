using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestAdapter.Framework
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
        public void LogInfo_Null_NonEmptyString()
        {
            Logger.LogInfo(null);

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfo_EmptyString_NonEmptyString()
        {
            Logger.LogInfo("");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogInfo_Whitespace_NonEmptyString()
        {
            Logger.LogInfo("\n");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogWarning_Foo_WarningAndFoo()
        {
            Logger.LogWarning("foo");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("Warning") && s.Contains("foo"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void LogError_Foo_ErrorAndFoo()
        {
            Logger.LogError("foo");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("ERROR") && s.Contains("foo"))),
                Times.Exactly(1));
        }

    }

}