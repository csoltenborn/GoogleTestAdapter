// This file has been modified by Microsoft on 6/2017.

using System.Xml;
using FluentAssertions;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Settings
{
    [TestClass]
    public class RunSettingsContainerTests
    {
        private RunSettingsContainer _container;
        private RunSettings _solutionSettings;
        private RunSettings _projectSettings1;
        private RunSettings _projectSettings2;

        [TestInitialize]
        public void Setup()
        {
            _solutionSettings = new RunSettings
            {
                ProjectRegex = null,
                AdditionalTestExecutionParam = "solution"
            };

            _projectSettings1 = new RunSettings
            {
                ProjectRegex = ".*PerformanceTests.exe",
                AdditionalTestExecutionParam = "project1"
            };

            _projectSettings2 = new RunSettings
            {
                ProjectRegex = ".*UnitTests.exe",
                AdditionalTestExecutionParam = "project2"
            };

            _container = new RunSettingsContainer(_solutionSettings);
            _container.ProjectSettings.Add(_projectSettings1);
            _container.ProjectSettings.Add(_projectSettings2);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForProject_InvalidProject_NullIsReturned()
        {
            var settings = _container.GetSettingsForExecutable("foo");
            settings.Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetSettingsForProject_ValidProject_ProjectSettingsAreReturned()
        {
            var settings = _container.GetSettingsForExecutable(@"C:\Users\chris\Desktop\MyPerformanceTests.exe");
            settings.Should().Be(_projectSettings1);
            settings.AdditionalTestExecutionParam.Should().Be("project1");

            settings = _container.GetSettingsForExecutable(@"TheUnitTests.exe");
            settings.Should().Be(_projectSettings2);
            settings.AdditionalTestExecutionParam.Should().Be("project2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetUnsetValuesFrom__ResultsInCorrectlyMergedContainer()
        {
            var solutionSettings = new RunSettings
            {
                ProjectRegex = null,
                AdditionalTestExecutionParam = "foo",
                BatchForTestSetup = "solution"
            };

            var projectSettings1 = new RunSettings
            {
                ProjectRegex = ".*PerformanceTests.exe",
                AdditionalTestExecutionParam = "foo",
                BatchForTestSetup = "project1"
            };

            var projectSettings2 = new RunSettings
            {
                ProjectRegex = ".*IntegrationTests.exe",
                AdditionalTestExecutionParam = "foo",
                BatchForTestSetup = "project2"
            };

            var container = new RunSettingsContainer(solutionSettings);
            container.ProjectSettings.Add(projectSettings1);
            container.ProjectSettings.Add(projectSettings2);

            _container.GetUnsetValuesFrom(container);

            _container.SolutionSettings.AdditionalTestExecutionParam.Should().Be("solution");
            _container.SolutionSettings.BatchForTestSetup.Should().Be("solution");

            _container.ProjectSettings.Should().HaveCount(3);
            _container.ProjectSettings[0].AdditionalTestExecutionParam.Should().Be("project1");
            _container.ProjectSettings[1].AdditionalTestExecutionParam.Should().Be("project2");
            _container.ProjectSettings[2].AdditionalTestExecutionParam.Should().Be("foo");
            _container.ProjectSettings[0].BatchForTestSetup.Should().Be("project1");
            _container.ProjectSettings[1].BatchForTestSetup.Should().BeNull();
            _container.ProjectSettings[2].BatchForTestSetup.Should().Be("project2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void LoadFromXml_UserSettings_AreLoadedCorrectly()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(TestResources.UserTestSettings);
            var navigator = xmlDocument.CreateNavigator();
            navigator.MoveToChild(Constants.RunSettingsName, "");
            navigator.MoveToChild(GoogleTestConstants.SettingsName, "");

            var runSettingsContainer = RunSettingsContainer.LoadFromXml(navigator);

            runSettingsContainer.Should().NotBeNull();
            runSettingsContainer.SolutionSettings.Should().NotBeNull();
            runSettingsContainer.ProjectSettings.Should().ContainSingle();

            runSettingsContainer.SolutionSettings.MaxNrOfThreads.Should().Be(3);
            runSettingsContainer.ProjectSettings[0].MaxNrOfThreads.Should().Be(4);
            runSettingsContainer.SolutionSettings.TraitsRegexesBefore.Should().Be("User///A,B");
            runSettingsContainer.ProjectSettings[0].TraitsRegexesBefore.Should().BeNull();
        }
    }
}