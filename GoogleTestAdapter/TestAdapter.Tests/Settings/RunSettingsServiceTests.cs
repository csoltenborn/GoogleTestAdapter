using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;

namespace GoogleTestAdapter.TestAdapter.Settings
{

    class RunSettingsServiceUnderTest : RunSettingsService
    {
        private string SolutionRunSettingsFile { get; }

        internal RunSettingsServiceUnderTest(IGlobalRunSettings globalRunSettings, string solutionRunSettingsFile) : base(globalRunSettings)
        {
            SolutionRunSettingsFile = solutionRunSettingsFile;
        }

        protected override string GetSolutionSettingsXmlFile()
        {
            return SolutionRunSettingsFile;
        }
    }

    [TestClass]
    public class RunSettingsServiceTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void Constructor__InstanceHasCorrectName()
        {
            Assert.AreEqual(GoogleTestConstants.SettingsName, new RunSettingsService(null).Name);
        }

        [TestMethod]
        public void AddRunSettings_UserSettingsWithoutRunSettingsNode_Warning()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IRunSettingsConfigurationInfo> mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(mockLogger, XmlFileBroken);

            XmlDocument xml = new XmlDocument();
            xml.Load(UserTestSettingsWithoutRunSettingsNode);

            service.AddRunSettings(xml, mockRunSettingsConfigInfo.Object, mockLogger.Object);

            mockLogger.Verify(l => l.Log(It.Is<MessageLevel>(ml => ml == MessageLevel.Warning), It.Is<string>(s => s.Contains("does not contain a RunSettings node"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void AddRunSettings_BrokenSolutionSettings_Warning()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IRunSettingsConfigurationInfo> mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(mockLogger, XmlFileBroken);

            XmlDocument xml = new XmlDocument();
            xml.Load(UserTestSettings);

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
        public void AddRunSettings_GlobalAndSolutionAndUserSettings_CorrectOverridingHierarchy()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IRunSettingsConfigurationInfo> mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(mockLogger, SolutionTestSettings);

            XmlDocument xml = new XmlDocument();
            xml.Load(UserTestSettings);

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

        private RunSettingsService SetupRunSettingsService(Mock<ILogger> mockLogger, string solutionRunSettingsFile)
        {
            RunSettings globalRunSettings = new RunSettings();
            globalRunSettings.AdditionalTestExecutionParam = "Global";
            globalRunSettings.NrOfTestRepetitions = 1;
            globalRunSettings.MaxNrOfThreads = 1;
            globalRunSettings.TraitsRegexesBefore = "Global";

            Mock<IGlobalRunSettings> mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalRunSettings);

            return new RunSettingsServiceUnderTest(mockGlobalRunSettings.Object, solutionRunSettingsFile);
        }

        private void AssertContainsSetting(XmlDocument xml, string nodeName, string value)
        {
            XmlNodeList list = xml.GetElementsByTagName(nodeName);
            Assert.AreEqual(1, list.Count);
            XmlNode node = list.Item(0);
            Assert.AreEqual(value, node.InnerText);
        }

    }

}