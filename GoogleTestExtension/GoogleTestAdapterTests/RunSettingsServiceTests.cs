using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using GoogleTestAdapterVSIX;
using GoogleTestAdapterVSIX.TestFrameworkIntegration;
using GoogleTestAdapterVSIX.TestFrameworkIntegration.Settings;

namespace GoogleTestAdapter
{

    [TestClass]
    public class RunSettingsServiceTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void RunSettingsService_Instantiation_HasCorrectName()
        {
            Assert.AreEqual(GoogleTestConstants.SettingsName, new RunSettingsService(null).Name);
        }

        [TestMethod]
        public void RunSettingsService_GentlyHandlesBrokenSolutionSettings()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IRunSettingsConfigurationInfo> mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(mockLogger);
            service.SolutionSettingsFile_ForTesting = XmlFileBroken;

            XmlDocument xml = new XmlDocument();
            xml.Load(UserTestSettings);

            service.AddRunSettings(xml, mockRunSettingsConfigInfo.Object, mockLogger.Object);

            // 1: from global, 2: from solution, 3: from user test settings
            AssertContainsSetting(xml, "AdditionalTestExecutionParam", "Global");
            AssertContainsSetting(xml, "BatchForTestTeardown", "User");
            AssertContainsSetting(xml, "NrOfTestRepetitions", "1");
            AssertContainsSetting(xml, "MaxNrOfThreads", "3");
            AssertContainsSetting(xml, "ShuffleTestsSeed", "3");
            AssertContainsSetting(xml, "ReportWaitPeriod", "3");

            mockLogger.Verify(l => l.Log(It.Is<MessageLevel>(ml => ml == MessageLevel.Warning), It.Is<string>(s => s.Contains("could not be parsed"))),
                Times.Exactly(1));
        }

        [TestMethod]
        public void RunSettingsService_CorrectOverridingHierarchy()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IRunSettingsConfigurationInfo> mockRunSettingsConfigInfo = new Mock<IRunSettingsConfigurationInfo>();

            RunSettingsService service = SetupRunSettingsService(mockLogger);
            service.SolutionSettingsFile_ForTesting = SolutionTestSettings;

            XmlDocument xml = new XmlDocument();
            xml.Load(UserTestSettings);

            service.AddRunSettings(xml, mockRunSettingsConfigInfo.Object, mockLogger.Object);

            // 1: from global, 2: from solution, 3: from user test settings
            AssertContainsSetting(xml, "AdditionalTestExecutionParam", "Global");
            AssertContainsSetting(xml, "BatchForTestSetup", "Solution");
            AssertContainsSetting(xml, "BatchForTestTeardown", "User");
            AssertContainsSetting(xml, "NrOfTestRepetitions", "2");
            AssertContainsSetting(xml, "MaxNrOfThreads", "3");
            AssertContainsSetting(xml, "ShuffleTestsSeed", "3");
            AssertContainsSetting(xml, "ReportWaitPeriod", "3");
        }

        private RunSettingsService SetupRunSettingsService(Mock<ILogger> mockLogger)
        {
            RunSettings globalRunSettings = new RunSettings();
            globalRunSettings.AdditionalTestExecutionParam = "Global";
            globalRunSettings.NrOfTestRepetitions = 1;
            globalRunSettings.MaxNrOfThreads = 1;
            globalRunSettings.ReportWaitPeriod = 1;

            Mock<IGlobalRunSettings> mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalRunSettings);

            return new RunSettingsService(mockGlobalRunSettings.Object);
        }

        private void AssertContainsSetting(XmlDocument xml, string nodeName, string value)
        {
            XmlNodeList list = xml.GetElementsByTagName(nodeName);
            Assert.IsTrue(list.Count == 1);
            XmlNode node = list.Item(0);
            Assert.AreEqual(value, node.InnerText);
        }

    }

}