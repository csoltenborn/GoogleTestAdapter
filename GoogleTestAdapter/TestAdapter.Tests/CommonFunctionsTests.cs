using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    public class CommonFunctionsTests
    {
        private readonly Mock<IRunSettings> _mockRunSettings = new Mock<IRunSettings>(MockBehavior.Strict);
        private readonly Mock<IMessageLogger> _mockMessageLogger = new Mock<IMessageLogger>();

        [TestCleanup]
        public void TearDown()
        {
            _mockRunSettings.Reset();
            _mockMessageLogger.Reset();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_RunSettingsThrow_LoggerIsNotNull()
        {
            CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object,
                out ILogger logger, out _);

            logger.Should().NotBeNull();
            _mockMessageLogger.Verify(l => l.SendMessage(
                It.Is<TestMessageLevel>(level => level == TestMessageLevel.Error),
                It.IsAny<string>()));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_SettingsEnvVarIsSet_SettingsAreReceivedFromEnvVar()
        {
            RunWithEnvVariable(
                CommonFunctions.GtaSettingsEnvVariable, TestResources.SolutionTestSettings,
                () =>
                {
                    CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object,
                        out _, out SettingsWrapper settings);

                    settings.Should().NotBeNull();
                    settings.BatchForTestSetup.Should().Be("Solution");
                });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_SettingsEnvVarIsNotSet_InfoIsLogged()
        {
            RunWithEnvVariable(
                CommonFunctions.GtaSettingsEnvVariable, null,
                () =>
                {
                    CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object,
                        out _, out SettingsWrapper settings);

                    settings.Should().NotBeNull();
                    _mockMessageLogger.Verify(l => l.SendMessage(
                        It.Is<TestMessageLevel>(ml => ml == TestMessageLevel.Informational),
                        It.Is<string>(s => s.Contains("No settings file provided through env variable"))));
                });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_SettingsEnvVarFileDoesNotExist_WarningIsLogged()
        {
            RunWithEnvVariable(
                CommonFunctions.GtaSettingsEnvVariable, "foobar",
                () =>
                {
                    CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object,
                        out _, out SettingsWrapper settings);

                    settings.Should().NotBeNull();
                    _mockMessageLogger.Verify(l => l.SendMessage(
                        It.Is<TestMessageLevel>(ml => ml == TestMessageLevel.Warning),
                        It.Is<string>(s => s.Contains("does not exist"))));
                });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_SettingsEnvVarFileWithoutSettingsNode_WarningIsLogged()
        {
            RunWithEnvVariable(
                CommonFunctions.GtaSettingsEnvVariable, TestResources.UserTestSettingsWithoutRunSettingsNode,
                () =>
                {
                    CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object,
                        out _, out SettingsWrapper settings);

                    settings.Should().NotBeNull();
                    _mockMessageLogger.Verify(l => l.SendMessage(
                        It.Is<TestMessageLevel>(ml => ml == TestMessageLevel.Warning),
                        It.Is<string>(s => s.Contains("could not be loaded"))));
                });
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_InvalidSettingsEnvVarFile_WarningIsLogged()
        {
            RunWithEnvVariable(
                CommonFunctions.GtaSettingsEnvVariable, TestResources.XmlFileBroken,
                () =>
                {
                    CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object,
                        out _, out SettingsWrapper settings);

                    settings.Should().NotBeNull();
                    _mockMessageLogger.Verify(l => l.SendMessage(
                        It.Is<TestMessageLevel>(ml => ml == TestMessageLevel.Error),
                        It.Is<string>(s => s.Contains("an exception occured while trying to read file"))));
                });
        }

        private void RunWithEnvVariable(string variable, string value, Action action)
        {
            string formerValue = Environment.GetEnvironmentVariable(variable);
            try
            {
                Environment.SetEnvironmentVariable(variable, value);
                action();
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, formerValue);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_DeprecatedSettingsAreNotSet_NoWarningIsLogged()
        {
            SetupRunSettingsAndCheckAssertions(
                mockGtaSettings =>
                {
                    mockGtaSettings.Setup(s => s.DebugMode).Returns((bool?) null);
                    mockGtaSettings.Setup(s => s.ShowReleaseNotes).Returns((bool?) null);
                }, 
                () => _mockMessageLogger.Verify(l => l.SendMessage(
                    It.Is<TestMessageLevel>(level => level > TestMessageLevel.Informational),
                    It.IsAny<string>()), Times.Never));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_DeprecatedSettingDebugMode_WarningIsLogged()
        {
            SetupRunSettingsAndCheckAssertions(
                mockRunSettings => mockRunSettings.Setup(s => s.DebugMode).Returns(true),
                () => _mockMessageLogger.Verify(l => l.SendMessage(
                    It.Is<TestMessageLevel>(level => level == TestMessageLevel.Warning),
                    It.Is<string>(s => s.Contains(nameof(RunSettings.DebugMode))))));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_DeprecatedSettingUseNewTestExecutionFramework_WarningIsLogged()
        {
            SetupRunSettingsAndCheckAssertions(
                mockRunSettings => mockRunSettings.Setup(s => s.UseNewTestExecutionFramework).Returns(true),
                () => _mockMessageLogger.Verify(l => l.SendMessage(
                    It.Is<TestMessageLevel>(level => level == TestMessageLevel.Warning),
                    It.Is<string>(s => s.Contains(nameof(RunSettings.UseNewTestExecutionFramework))))));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CreateEnvironment_DeprecatedSettingShowReleaseNotes_WarningIsLogged()
        {
            SetupRunSettingsAndCheckAssertions(
                mockRunSettings => mockRunSettings.Setup(s => s.ShowReleaseNotes).Returns(true),
                () => _mockMessageLogger.Verify(l => l.SendMessage(
                    It.Is<TestMessageLevel>(level => level == TestMessageLevel.Warning),
                    It.Is<string>(s => s.Contains(nameof(RunSettings.ShowReleaseNotes))))));
        }

        private void SetupRunSettingsAndCheckAssertions(Action<Mock<RunSettings>> setup, Action assertions)
        {
            var mockSettingsProvider = new Mock<RunSettingsProvider>();
            _mockRunSettings.Setup(rs => rs.GetSettings(It.Is<string>(s => s == GoogleTestConstants.SettingsName)))
                .Returns(mockSettingsProvider.Object);

            var mockRunSettingsContainer = new Mock<RunSettingsContainer>();
            mockSettingsProvider.Setup(p => p.SettingsContainer).Returns(mockRunSettingsContainer.Object);

            var mockGtaSettings = new Mock<RunSettings>();
            mockRunSettingsContainer.Setup(c => c.SolutionSettings).Returns(mockGtaSettings.Object);

            setup(mockGtaSettings);

            CommonFunctions.CreateEnvironment(_mockRunSettings.Object, _mockMessageLogger.Object, out _, out _);

            assertions();
        }

    }
}