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
                AdditionalTestDiscoveryParam = "solution",
                AdditionalTestExecutionParam = "solution"
            };

            _projectSettings1 = new RunSettings
            {
                ProjectRegex = ".*PerformanceTests.exe",
                AdditionalTestDiscoveryParam = "project1",
                AdditionalTestExecutionParam = "project1"
            };

            _projectSettings2 = new RunSettings
            {
                ProjectRegex = ".*UnitTests.exe",
                AdditionalTestDiscoveryParam = "project2",
                AdditionalTestExecutionParam = "project2"
            };

            _container = new RunSettingsContainer { SolutionSettings = _solutionSettings };
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
            settings.AdditionalTestDiscoveryParam.Should().Be("project1");
            settings.AdditionalTestExecutionParam.Should().Be("project1");

            settings = _container.GetSettingsForExecutable(@"TheUnitTests.exe");
            settings.Should().Be(_projectSettings2);
            settings.AdditionalTestDiscoveryParam.Should().Be("project2");
            settings.AdditionalTestExecutionParam.Should().Be("project2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetUnsetValuesFrom__ResultsInCorrectlyMergedContainer()
        {
            var solutionSettings = new RunSettings
            {
                ProjectRegex = null,
                AdditionalTestDiscoveryParam = "bar",
                AdditionalTestExecutionParam = "foo",
                BatchForTestSetup = "solution"
            };

            var projectSettings1 = new RunSettings
            {
                ProjectRegex = ".*PerformanceTests.exe",
                AdditionalTestDiscoveryParam = "bar",
                AdditionalTestExecutionParam = "foo",
                BatchForTestSetup = "project1"
            };

            var projectSettings2 = new RunSettings
            {
                ProjectRegex = ".*IntegrationTests.exe",
                AdditionalTestDiscoveryParam = "bar",
                AdditionalTestExecutionParam = "foo",
                BatchForTestSetup = "project2"
            };

            var container = new RunSettingsContainer { SolutionSettings = solutionSettings };
            container.ProjectSettings.Add(projectSettings1);
            container.ProjectSettings.Add(projectSettings2);

            _container.GetUnsetValuesFrom(container);

            _container.SolutionSettings.AdditionalTestDiscoveryParam.Should().Be("solution");
            _container.SolutionSettings.AdditionalTestExecutionParam.Should().Be("solution");
            _container.SolutionSettings.BatchForTestSetup.Should().Be("solution");

            _container.ProjectSettings.Count.Should().Be(3);
            _container.ProjectSettings[0].AdditionalTestDiscoveryParam.Should().Be("project1");
            _container.ProjectSettings[1].AdditionalTestDiscoveryParam.Should().Be("project2");
            _container.ProjectSettings[2].AdditionalTestDiscoveryParam.Should().Be("bar");
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
            runSettingsContainer.ProjectSettings.Count.Should().Be(1);

            runSettingsContainer.SolutionSettings.MaxNrOfThreads.Should().Be(3);
            runSettingsContainer.ProjectSettings[0].MaxNrOfThreads.Should().Be(4);
            runSettingsContainer.SolutionSettings.TraitsRegexesBefore.Should().Be("User///A,B");
            runSettingsContainer.ProjectSettings[0].TraitsRegexesBefore.Should().BeNull();
        }

    }

}