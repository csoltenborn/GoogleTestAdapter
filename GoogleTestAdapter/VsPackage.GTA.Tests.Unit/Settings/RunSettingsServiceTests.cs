// This file has been modified by Microsoft on 7/2017.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using FluentAssertions;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.VsPackage.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;
// ReSharper disable PossibleNullReferenceException

namespace GoogleTestAdapter.VsPackage.Settings
{

    [TestClass]
    public class RunSettingsServiceTests : TestsBase
    {
        private const string GlobalWorkingDir = "GlobalWorkingDir";
        private const string SolutionSolutionWorkingDir = "SolutionSolutionWorkingDir";
        private const string SolutionProject1WorkingDir = "SolutionProject1WorkingDir";
        private const string SolutionProject2WorkingDir = "SolutionProject2";
        private const string UserSolutionWorkingDir = "UserSolutionWorkingDir";
        private const string UserProject1WorkingDir = "UserProject1WorkingDir";
        private const string UserProject3WorkingDir = "UserProject3WorkingDir";

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
            AssertContainsSetting(xml, "TraitsRegexesBefore", "User///A,B");

            mockLogger.Verify(l => l.Log(It.Is<MessageLevel>(ml => ml == MessageLevel.Warning), It.Is<string>(s => s.Contains("could not be parsed"))),
                Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_CompleteSettings_BasicChecks()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, SolutionProject1WorkingDir, SolutionProject2WorkingDir,
                UserSolutionWorkingDir, UserProject1WorkingDir, UserProject3WorkingDir);

            resultingContainer.Should().NotBeNull();
            resultingContainer.SolutionSettings.Should().NotBeNull();
            resultingContainer.ProjectSettings.Count.Should().Be(3);

            resultingContainer.GetSettingsForExecutable("project1").Should().NotBeNull();
            resultingContainer.GetSettingsForExecutable("project2").Should().NotBeNull();
            resultingContainer.GetSettingsForExecutable("project3").Should().NotBeNull();
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_CompleteSettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, SolutionProject1WorkingDir, SolutionProject2WorkingDir,
                UserSolutionWorkingDir, UserProject1WorkingDir, UserProject3WorkingDir);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(SolutionProject2WorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserProject3WorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_NoSettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, null, null,
                null, null, null);

            resultingContainer.GetSettingsForExecutable("project1").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("project2").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("project3").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_EmptySettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, "", "",
                null, "", "");

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(GlobalWorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(GlobalWorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(GlobalWorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_CompleteSolutionSettingsNoProjectSettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, SolutionProject1WorkingDir, SolutionProject2WorkingDir,
                null, null, null);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(SolutionProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(SolutionProject2WorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_NoSolutionSettingsCompleteProjectSettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, null, null,
                UserSolutionWorkingDir, UserProject1WorkingDir, UserProject3WorkingDir);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserProject3WorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_CompleteSolutionSettingsEmptyProjectSettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, SolutionProject1WorkingDir, SolutionProject2WorkingDir,
                null, "", "");

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(SolutionProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(SolutionProject2WorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(SolutionSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_EmptySolutionSettingsCompleteProjectSettings_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, "", "",
                UserSolutionWorkingDir, UserProject1WorkingDir, UserProject3WorkingDir);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserProject3WorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings1_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, null, "",
                UserSolutionWorkingDir, "", null);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings2_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, SolutionProject1WorkingDir, null,
                null, "", UserProject3WorkingDir);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(SolutionProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserProject3WorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings3_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, "", SolutionProject2WorkingDir,
                null, UserProject1WorkingDir, null);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(SolutionProject2WorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings4_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, SolutionProject1WorkingDir, "",
                UserSolutionWorkingDir, null, "");

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(SolutionProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings5_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                SolutionSolutionWorkingDir, "", SolutionProject2WorkingDir,
                UserSolutionWorkingDir, null, UserProject3WorkingDir);

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(SolutionProject2WorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserProject3WorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings6_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, "", null,
                null, UserProject1WorkingDir, "");

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").Should().BeNull();
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(GlobalWorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_MixedSettings7_()
        {
            var resultingContainer = SetupFinalRunSettingsContainer(
                null, null, SolutionProject2WorkingDir,
                UserSolutionWorkingDir, UserProject1WorkingDir, "");

            resultingContainer.GetSettingsForExecutable("project1").WorkingDir.Should().Be(UserProject1WorkingDir);
            resultingContainer.GetSettingsForExecutable("project2").WorkingDir.Should().Be(SolutionProject2WorkingDir);
            resultingContainer.GetSettingsForExecutable("project3").WorkingDir.Should().Be(UserSolutionWorkingDir);
            resultingContainer.GetSettingsForExecutable("not_matched").Should().BeNull();
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

            var mockGlobalRunSettings = new Mock<IGlobalRunSettings2>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalRunSettings);

            return new RunSettingsServiceUnderTest(mockGlobalRunSettings.Object, solutionRunSettingsFile);
        }

        private void AssertContainsSetting(XmlDocument xml, string nodeName, string value)
        {
            XmlNode solutionSettingsNode = xml.GetElementsByTagName("SolutionSettings").Item(0);
            XmlNodeList list = solutionSettingsNode.SelectNodes($"Settings/{nodeName}");

            list.Should().HaveCount(1, $"node {nodeName} should exist only once. XML Document:{Environment.NewLine}{ToFormattedString(xml, 4)}");

            XmlNode node = list.Item(0);
            node.Should().NotBeNull();
            // ReSharper disable once PossibleNullReferenceException
            node.InnerText.Should().BeEquivalentTo(value);
        }

        public static string ToFormattedString(XmlDocument xml, int indentation)
        {
            string xmlAsString;
            using (var sw = new StringWriter())
            {
                using (var xw = new XmlTextWriter(sw))
                {
                    xw.Formatting = Formatting.Indented;
                    xw.Indentation = indentation;
                    xml.WriteContentTo(xw);
                }
                xmlAsString = sw.ToString();
            }
            return xmlAsString;
        }

        private RunSettingsContainer SetupFinalRunSettingsContainer(
            string solutionSolutionWorkingDir, string solutionProject1WorkingDir, string solutionProject2WorkingDir, 
            string userSolutionWorkingDir, string userProject1WorkingDir, string userProject3WorkingDir)
        {
            var globalSettings = new RunSettings { ProjectRegex = null, WorkingDir = GlobalWorkingDir };
            var mockGlobalRunSettings = new Mock<IGlobalRunSettings2>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalSettings);

            var solutionSettingsContainer = SetupSettingsContainer(solutionSolutionWorkingDir, solutionProject1WorkingDir, solutionProject2WorkingDir, null);
            var solutionSettingsNavigator = EmbedSettingsIntoRunSettings(solutionSettingsContainer);
            var solutionSettingsFile = SerializeSolutionSettings(solutionSettingsNavigator);

            var userSettingsContainer = SetupSettingsContainer(userSolutionWorkingDir, userProject1WorkingDir, null, userProject3WorkingDir);
            var userSettingsNavigator = EmbedSettingsIntoRunSettings(userSettingsContainer);

            IXPathNavigable navigable;
            try
            {
                var serviceUnderTest = new RunSettingsServiceUnderTest(mockGlobalRunSettings.Object, solutionSettingsFile);
                navigable = serviceUnderTest.AddRunSettings(userSettingsNavigator,
                    new Mock<IRunSettingsConfigurationInfo>().Object, new Mock<ILogger>().Object);
            }
            finally
            {
                File.Delete(solutionSettingsFile);
            }

            var navigator = navigable.CreateNavigator();
            navigator.MoveToChild(Constants.RunSettingsName, "");
            navigator.MoveToChild(GoogleTestConstants.SettingsName, "");

            return RunSettingsContainer.LoadFromXml(navigator);
        }

        private RunSettingsContainer SetupSettingsContainer(string solutionWorkingDir, 
            string project1WorkingDir, string project2WorkingDir, string project3WorkingDir)
        {
            var settingsContainer = new RunSettingsContainer
            {
                SolutionSettings = new RunSettings
                {
                    ProjectRegex = null,
                    WorkingDir = solutionWorkingDir
                },
                ProjectSettings = new List<RunSettings>()
            };

            AddProjectSettings(settingsContainer, "project1", project1WorkingDir);
            AddProjectSettings(settingsContainer, "project2", project2WorkingDir);
            AddProjectSettings(settingsContainer, "project3", project3WorkingDir);

            return settingsContainer;
        }

        private static void AddProjectSettings(RunSettingsContainer settingsContainer, string project, string workingDir)
        {
            if (workingDir == null)
                return;

            settingsContainer.ProjectSettings.Add(new RunSettings
            {
                ProjectRegex = project,
                WorkingDir = workingDir == "" ? null : workingDir
            });
        }

        private static XPathNavigator EmbedSettingsIntoRunSettings(RunSettingsContainer settingsContainer)
        {
            var settingsDocument = new XmlDocument();
            XmlDeclaration xmlDeclaration = settingsDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = settingsDocument.DocumentElement;
            settingsDocument.InsertBefore(xmlDeclaration, root);

            XmlElement runSettingsNode = settingsDocument.CreateElement("", Constants.RunSettingsName, "");
            settingsDocument.AppendChild(runSettingsNode);

            var settingsNavigator = settingsDocument.CreateNavigator();
            settingsNavigator.MoveToChild(Constants.RunSettingsName, "");
            settingsNavigator.AppendChild(settingsContainer.ToXml().CreateNavigator());
            settingsNavigator.MoveToRoot();

            return settingsNavigator;
        }

        private string SerializeSolutionSettings(XPathNavigator settingsNavigator)
        {
            string settingsFile = Path.GetTempFileName();

            var document = new XmlDocument();
            document.LoadXml(settingsNavigator.OuterXml);
            document.Save(settingsFile);

            return settingsFile;
        }

    }

}