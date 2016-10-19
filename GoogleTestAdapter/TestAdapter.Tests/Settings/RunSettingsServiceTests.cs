using System.Xml;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    [TestClass]
    public class RunSettingsServiceTests : TestsBase
    {

        private class RunSettingsServiceUnderTest : RunSettingsService
        {
            private readonly string _solutionRunSettingsFile;

            internal RunSettingsServiceUnderTest(IGlobalRunSettings globalRunSettings, string solutionRunSettingsFile) : base(globalRunSettings)
            {
                _solutionRunSettingsFile = solutionRunSettingsFile;
            }

            protected override string GetSolutionSettingsXmlFile()
            {
                return _solutionRunSettingsFile;
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Constructor__InstanceHasCorrectName()
        {
            new RunSettingsService(null).Name.Should().Be(GoogleTestConstants.SettingsName);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_UserSettingsWithoutRunSettingsNode_Warning()
        {
            var mockLogger = new Mock<ILogger>();
            var mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(TestResources.XmlFileBroken);

            var xml = new XmlDocument();
            xml.Load(TestResources.UserTestSettingsWithoutRunSettingsNode);

            service.AddRunSettings(xml, mockRunSettingsConfigInfo.Object, mockLogger.Object);

            mockLogger.Verify(l => l.Log(It.Is<MessageLevel>(ml => ml == MessageLevel.Warning), It.Is<string>(s => s.Contains("does not contain a RunSettings node"))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_BrokenSolutionSettings_Warning()
        {
            var mockLogger = new Mock<ILogger>();
            var mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(TestResources.XmlFileBroken);

            var xml = new XmlDocument();
            xml.Load(TestResources.UserTestSettings);

            service.AddRunSettings(xml, mockRunSettingsConfigInfo.Object, mockLogger.Object);

            // 1: from global, 2: from solution, 3, ShuffleTests: from user test settings
            AssertContainsSetting(xml, "AdditionalTestExecutionParam", "Global");
            AssertContainsSetting(xml, "ShuffleTests", "true");
            AssertContainsSetting(xml, "NrOfTestRepetitions", "1");
            AssertContainsSetting(xml, "MaxNrOfThreads", "3");
            AssertContainsSetting(xml, "ShuffleTestsSeed", "3");
            AssertContainsSetting(xml, "TraitsRegexesBefore", "User");

            mockLogger.Verify(l => l.Log(It.Is<MessageLevel>(ml => ml == MessageLevel.Warning), It.Is<string>(s => s.Contains("could not be parsed"))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_GlobalAndSolutionAndUserSettings_CorrectOverridingHierarchy()
        {
            var mockLogger = new Mock<ILogger>();
            var mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(TestResources.SolutionTestSettings);

            var xml = new XmlDocument();
            xml.Load(TestResources.UserTestSettings);

            service.AddRunSettings(xml, mockRunSettingsConfigInfo.Object, mockLogger.Object);

            // 1: from global, 2: from solution, 3, ShuffleTests: from user test settings
            AssertContainsSetting(xml, "AdditionalTestExecutionParam", "Global");
            AssertContainsSetting(xml, "BatchForTestSetup", "Solution");
            AssertContainsSetting(xml, "ShuffleTests", "true");
            AssertContainsSetting(xml, "NrOfTestRepetitions", "2");
            AssertContainsSetting(xml, "MaxNrOfThreads", "3");
            AssertContainsSetting(xml, "ShuffleTestsSeed", "3");
            AssertContainsSetting(xml, "TraitsRegexesBefore", "User");
        }

        private RunSettingsService SetupRunSettingsService(string solutionRunSettingsFile)
        {
            var globalRunSettings = new RunSettings
            {
                AdditionalTestExecutionParam = "Global",
                NrOfTestRepetitions = 1,
                MaxNrOfThreads = 1,
                TraitsRegexesBefore = "Global"
            };

            Mock<IGlobalRunSettings> mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalRunSettings);

            return new RunSettingsServiceUnderTest(mockGlobalRunSettings.Object, solutionRunSettingsFile);
        }

        private void AssertContainsSetting(XmlDocument xml, string nodeName, string value)
        {
            XmlNodeList list = xml.GetElementsByTagName(nodeName);
            list.Count.Should().Be(1);

            XmlNode node = list.Item(0);
            node.Should().NotBeNull();
            // ReSharper disable once PossibleNullReferenceException
            node.InnerText.Should().Be(value);
        }

    }

}