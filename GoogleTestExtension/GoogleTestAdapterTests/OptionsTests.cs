using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Xml;

namespace GoogleTestAdapter
{
    [TestClass]
    public class OptionsTests : AbstractGoogleTestExtensionTests
    {

        private Mock<IXmlOptions> MockXmlOptions { get; } = new Mock<IXmlOptions>();
        private AbstractOptions TheOptions { get; set; }


        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            TheOptions = new Options(MockXmlOptions.Object, MockLogger.Object);
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockXmlOptions.Reset();
        }


        [TestMethod]
        public void NrOfTestRepitionsHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(-2);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(0);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);
            Assert.AreEqual(4711, TheOptions.NrOfTestRepetitions);
        }

        [TestMethod]
        public void ShuffleTestsSeedHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(-1);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(1000000);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);
            Assert.AreEqual(4711, TheOptions.ShuffleTestsSeed);
        }

        [TestMethod]
        public void MaxNrOfThreadsHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.MaxNrOfThreads).Returns(-1);
            Assert.AreEqual(Environment.ProcessorCount, TheOptions.MaxNrOfThreads);

            MockXmlOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount + 1);
            Assert.AreEqual(Environment.ProcessorCount, TheOptions.MaxNrOfThreads);

            if (Environment.ProcessorCount > 1)
            {
                MockXmlOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount - 1);
                Assert.AreEqual(Environment.ProcessorCount - 1, TheOptions.MaxNrOfThreads);
            }
        }

        [TestMethod]
        public void ReportWaitPeriodHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.ReportWaitPeriod).Returns(-1);
            Assert.AreEqual(Options.OptionReportWaitPeriodDefaultValue, TheOptions.ReportWaitPeriod);

            MockXmlOptions.Setup(o => o.ReportWaitPeriod).Returns(4711);
            Assert.AreEqual(4711, TheOptions.ReportWaitPeriod);
        }

        [TestMethod]
        public void AdditionalTestParameter_PlaceholdersAreTreatedCorrectly()
        {
            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder);
            string result = TheOptions.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir", result);

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder + " " + Options.TestDirPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir mydir", result);

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder.ToLower());
            result = TheOptions.GetUserParameters("", "mydir", 0);
            Assert.AreEqual(Options.TestDirPlaceholder.ToLower(), result);

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.ThreadIdPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("4711", result);

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder + ", " + Options.ThreadIdPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("mydir, 4711", result);
        }

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
            Mock<IGlobalRunSettings> mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
            RunSettings globalRunSettings = new RunSettings();
            globalRunSettings.AdditionalTestExecutionParam = "Global";
            globalRunSettings.NrOfTestRepetitions = 1;
            globalRunSettings.MaxNrOfThreads = 1;
            globalRunSettings.ReportWaitPeriod = 1;
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