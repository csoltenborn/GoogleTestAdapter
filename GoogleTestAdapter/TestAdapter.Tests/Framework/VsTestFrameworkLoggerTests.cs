using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    [TestClass]
    public class VsTestFrameworkLoggerTests : TestAdapterTestsBase
    {

        private VsTestFrameworkLogger _logger;

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            _logger = new VsTestFrameworkLogger(MockVsLogger.Object, () => MockOptions.Object.OutputMode, 
                () => MockOptions.Object.TimestampMode, () => MockOptions.Object.SeverityMode, () => MockOptions.Object.PrefixOutputWithGta);
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void LogInfo_Null_NonEmptyString()
        {
            _logger.LogInfo(null);

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void LogInfo_EmptyString_NonEmptyString()
        {
            _logger.LogInfo("");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void LogInfo_Whitespace_NonEmptyString()
        {
            _logger.LogInfo("\n");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Informational),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void LogWarning_Foo_WarningAndFoo()
        {
            _logger.LogWarning("foo");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning),
                It.Is<string>(s => s.Contains("Warning") && s.Contains("foo"))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void LogError_Foo_ErrorAndFoo()
        {
            _logger.LogError("foo");

            MockVsLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Error),
                It.Is<string>(s => s.Contains("ERROR") && s.Contains("foo"))),
                Times.Exactly(1));
        }

    }

}