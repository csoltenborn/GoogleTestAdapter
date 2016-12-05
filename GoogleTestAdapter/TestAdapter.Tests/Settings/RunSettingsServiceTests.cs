using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using FluentAssertions;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;
// ReSharper disable PossibleNullReferenceException

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

        [TestMethod]
        [TestCategory(Unit)]
        public void AddRunSettings_ComplexConfiguration_IsMergedCorrectly()
        {
            string global = "GlobalSettings";
            string solutionSolution = "solutionSolution";
            string solutionProject1 = "solutionProject1";
            string solutionProject2 = "solutionProject2";
            string userSolution = "userSolution";
            string userProject1 = "userProject1";
            string userProject3 = "userProject3";

            var solutionSettingsContainer = new RunSettingsContainer
            {
                SolutionSettings = new RunSettings
                {
                    ProjectRegex = null,
                    AdditionalTestExecutionParam = solutionSolution,
                    PathExtension = solutionSolution,
                    TestDiscoveryRegex = solutionSolution,
                    TraitsRegexesAfter = solutionSolution,
                    WorkingDir = solutionSolution,
                    MaxNrOfThreads = 1,
                },
                ProjectSettings = new List<RunSettings>
                {
                    new RunSettings
                    {
                        ProjectRegex = "project1",
                        AdditionalTestExecutionParam = solutionProject1,
                        BatchForTestTeardown = solutionProject1,
                        PathExtension = solutionProject1,
                        TestDiscoveryRegex = solutionProject1,
                        NrOfTestRepetitions = 2,
                        ShuffleTestsSeed = 2
                    },
                    new RunSettings
                    {
                        ProjectRegex = "project2",
                        AdditionalTestExecutionParam = solutionProject2,
                        BatchForTestTeardown = solutionProject2,
                        TestNameSeparator = solutionProject2,
                        TraitsRegexesAfter = solutionProject2,
                        WorkingDir = solutionProject2,
                        NrOfTestRepetitions = 3,
                    }
                }
            };

            var userSettingsContainer = new RunSettingsContainer
            {
                SolutionSettings = new RunSettings
                {
                    ProjectRegex = null,
                    BatchForTestSetup = userSolution,
                    BatchForTestTeardown = userSolution,
                    TestDiscoveryRegex = userSolution,
                    TraitsRegexesAfter = userSolution,
                    MaxNrOfThreads = 4,
                    ShuffleTestsSeed = 4
                },
                ProjectSettings = new List<RunSettings>
                {
                    new RunSettings
                    {
                        ProjectRegex = "project1",
                        BatchForTestTeardown = userProject1,
                        PathExtension = userProject1,
                        TestNameSeparator = userProject1,
                        WorkingDir = userProject1,
                        MaxNrOfThreads = 5,
                        ShuffleTestsSeed = 5
                    },
                    new RunSettings
                    {
                        ProjectRegex = "project3",
                        AdditionalTestExecutionParam = userProject3,
                        BatchForTestTeardown = userProject3,
                        TestDiscoveryRegex = userProject3,
                        TestNameSeparator = userProject3,
                        TraitsRegexesBefore = userProject3,
                        MaxNrOfThreads = 6,
                    }
                }
            };

            var globalSettings = new RunSettings
            {
                ProjectRegex = null,
                AdditionalTestExecutionParam = global,
                BatchForTestSetup = global,
                BatchForTestTeardown = global,
                PathExtension = global,
                TestDiscoveryRegex = global,
                TestNameSeparator = global,
                TraitsRegexesAfter = global,
                TraitsRegexesBefore = global,
                WorkingDir = global,
                MaxNrOfThreads = 0,
                NrOfTestRepetitions = 0,
                ShuffleTestsSeed = 0
            };

            var mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalSettings);

            var userSettingsNavigator = EmbedSettingsIntoRunSettings(userSettingsContainer);

            string solutionSettingsFile = SerializeSettingsContainer(solutionSettingsContainer);
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
            navigator.MoveToChild("RunSettings", "");
            navigator.MoveToChild(GoogleTestConstants.SettingsName, "");
            var resultingContainer = RunSettingsContainer.LoadFromXml(navigator.ReadSubtree());

            resultingContainer.Should().NotBeNull();
            resultingContainer.SolutionSettings.Should().NotBeNull();
            resultingContainer.ProjectSettings.Count.Should().Be(3);

            resultingContainer.SolutionSettings.AdditionalTestExecutionParam.Should().Be(solutionSolution);
            resultingContainer.SolutionSettings.BatchForTestSetup.Should().Be(userSolution);
            resultingContainer.SolutionSettings.BatchForTestTeardown.Should().Be(userSolution);
            resultingContainer.SolutionSettings.PathExtension.Should().Be(solutionSolution);
            resultingContainer.SolutionSettings.TestDiscoveryRegex.Should().Be(userSolution);
            resultingContainer.SolutionSettings.TestNameSeparator.Should().Be(global);
            resultingContainer.SolutionSettings.TraitsRegexesAfter.Should().Be(userSolution);
            resultingContainer.SolutionSettings.TraitsRegexesBefore.Should().Be(global);
            resultingContainer.SolutionSettings.WorkingDir.Should().Be(solutionSolution);
            resultingContainer.SolutionSettings.MaxNrOfThreads.Should().Be(4);
            resultingContainer.SolutionSettings.MaxNrOfThreads.Should().Be(4);
            resultingContainer.SolutionSettings.NrOfTestRepetitions.Should().Be(0);

            var projectContainer = resultingContainer.GetSettingsForExecutable("project1");
            projectContainer.Should().NotBeNull();
            projectContainer.AdditionalTestExecutionParam.Should().Be(solutionProject1);
            projectContainer.BatchForTestSetup.Should().Be(userSolution);
            projectContainer.BatchForTestTeardown.Should().Be(userProject1);
            projectContainer.PathExtension.Should().Be(userProject1);
            projectContainer.TestDiscoveryRegex.Should().Be(userSolution);
            projectContainer.TestNameSeparator.Should().Be(userProject1);
            projectContainer.TraitsRegexesAfter.Should().Be(userSolution);
            projectContainer.TraitsRegexesBefore.Should().Be(global);
            projectContainer.WorkingDir.Should().Be(userProject1);
            projectContainer.MaxNrOfThreads.Should().Be(5);
            projectContainer.MaxNrOfThreads.Should().Be(5);
            projectContainer.NrOfTestRepetitions.Should().Be(2);

            projectContainer = resultingContainer.GetSettingsForExecutable("project2");
            projectContainer.Should().NotBeNull();
            projectContainer.AdditionalTestExecutionParam.Should().Be(solutionProject2);
            projectContainer.BatchForTestSetup.Should().Be(global);
            projectContainer.BatchForTestTeardown.Should().Be(solutionProject2);
            projectContainer.PathExtension.Should().Be(solutionSolution);
            projectContainer.TestDiscoveryRegex.Should().Be(solutionSolution);
            projectContainer.TestNameSeparator.Should().Be(solutionProject2);
            projectContainer.TraitsRegexesAfter.Should().Be(solutionProject2);
            projectContainer.TraitsRegexesBefore.Should().Be(global);
            projectContainer.WorkingDir.Should().Be(solutionProject2);
            projectContainer.MaxNrOfThreads.Should().Be(1);
            projectContainer.MaxNrOfThreads.Should().Be(1);
            projectContainer.NrOfTestRepetitions.Should().Be(3);

            projectContainer = resultingContainer.GetSettingsForExecutable("project3");
            projectContainer.Should().NotBeNull();
            projectContainer.AdditionalTestExecutionParam.Should().Be(userProject3);
            projectContainer.BatchForTestSetup.Should().Be(userSolution);
            projectContainer.BatchForTestTeardown.Should().Be(userProject3);
            projectContainer.PathExtension.Should().Be(global);
            projectContainer.TestDiscoveryRegex.Should().Be(userProject3);
            projectContainer.TestNameSeparator.Should().Be(userProject3);
            projectContainer.TraitsRegexesAfter.Should().Be(userSolution);
            projectContainer.TraitsRegexesBefore.Should().Be(userProject3);
            projectContainer.WorkingDir.Should().Be(global);
            projectContainer.MaxNrOfThreads.Should().Be(6);
            projectContainer.MaxNrOfThreads.Should().Be(6);
            projectContainer.NrOfTestRepetitions.Should().Be(0);
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

            var mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
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

        private static XPathNavigator EmbedSettingsIntoRunSettings(RunSettingsContainer settingsContainer)
        {
            var settingsDocument = new XmlDocument();
            XmlDeclaration xmlDeclaration = settingsDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = settingsDocument.DocumentElement;
            settingsDocument.InsertBefore(xmlDeclaration, root);

            XmlElement runSettingsNode = settingsDocument.CreateElement("", "RunSettings", "");
            settingsDocument.AppendChild(runSettingsNode);

            var settingsNavigator = settingsDocument.CreateNavigator();
            settingsNavigator.MoveToChild("RunSettings", "");
            settingsNavigator.AppendChild(settingsContainer.ToXml().CreateNavigator());
            settingsNavigator.MoveToRoot();

            return settingsNavigator;
        }

        private string SerializeSettingsContainer(RunSettingsContainer settingsContainer)
        {
            var settingsNavigator = EmbedSettingsIntoRunSettings(settingsContainer);

            var finalDocument = new XmlDocument();
            finalDocument.LoadXml(settingsNavigator.OuterXml);
            string targetFile = Path.GetTempFileName();
            finalDocument.Save(targetFile);

            return targetFile;
        }

    }

}