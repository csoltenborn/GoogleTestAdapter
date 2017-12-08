// This file has been modified by Microsoft on 7/2017.

using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{

    [TestClass]
    public class CommonFunctionsTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_RunSettingsThrow_LoggerIsNotNull()
        {
            var mockRunSettings = new Mock<IRunSettings>(MockBehavior.Strict);
            var mockMessageLogger = new Mock<IMessageLogger>();

            CommonFunctions.CreateEnvironment(mockRunSettings.Object, mockMessageLogger.Object, out ILogger logger, out SettingsWrapper settings);

            logger.Should().NotBeNull();
            settings.Should().NotBeNull();
            mockMessageLogger.Verify(l => l.SendMessage(It.Is<TestMessageLevel>(level => level == TestMessageLevel.Error), It.IsAny<string>()), Times.Once);
        }

    }
}