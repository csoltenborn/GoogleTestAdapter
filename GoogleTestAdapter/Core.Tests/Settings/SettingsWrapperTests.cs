using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Settings
{

    [TestClass]
    public class SettingsWrapperTests : TestsBase
    {

        private Mock<RunSettings> MockXmlOptions { get; } = new Mock<RunSettings>();
        private SettingsWrapper TheOptions { get; set; }


        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            var containerMock = new Mock<IGoogleTestAdapterSettingsContainer>();
            containerMock.Setup(c => c.SolutionSettings).Returns(MockXmlOptions.Object);
            containerMock.Setup(c => c.GetSettingsForExecutable(It.IsAny<string>())).Returns(MockXmlOptions.Object);
            TheOptions = new SettingsWrapper(containerMock.Object)
            {
                RegexTraitParser = new RegexTraitParser(TestEnvironment.Logger)
            };
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockXmlOptions.Reset();
        }


        [TestMethod]
        [TestCategory(Unit)]
        public void NrOfTestRepitions_InvalidValue_ReturnsDefaultValue()
        {
            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(-2);
            TheOptions.NrOfTestRepetitions.Should().Be(SettingsWrapper.OptionNrOfTestRepetitionsDefaultValue);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(0);

            TheOptions.NrOfTestRepetitions.Should().Be(SettingsWrapper.OptionNrOfTestRepetitionsDefaultValue);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);
            TheOptions.NrOfTestRepetitions.Should().Be(4711);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ShuffleTestsSeed_InvalidValue_ReturnsDefaultValue()
        {
            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(-1);
            TheOptions.ShuffleTestsSeed.Should().Be(SettingsWrapper.OptionShuffleTestsSeedDefaultValue);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(1000000);
            TheOptions.ShuffleTestsSeed.Should().Be(SettingsWrapper.OptionShuffleTestsSeedDefaultValue);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);
            TheOptions.ShuffleTestsSeed.Should().Be(4711);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void MaxNrOfThreads_InvalidValue_ReturnsDefaultValue()
        {
            MockXmlOptions.Setup(o => o.MaxNrOfThreads).Returns(-1);
            TheOptions.MaxNrOfThreads.Should().Be(Environment.ProcessorCount);

            if (Environment.ProcessorCount > 1)
            {
                MockXmlOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount - 1);
                TheOptions.MaxNrOfThreads.Should().Be(Environment.ProcessorCount - 1);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AdditionalTestExecutionParam__PlaceholdersAreTreatedCorrectly()
        {
            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(SettingsWrapper.TestDirPlaceholder);
            string result = TheOptions.GetUserParameters("", "mydir", 0);
            result.Should().Be("mydir");

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(SettingsWrapper.TestDirPlaceholder + " " + SettingsWrapper.TestDirPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 0);
            result.Should().Be("mydir mydir");

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(SettingsWrapper.TestDirPlaceholder.ToLower());
            result = TheOptions.GetUserParameters("", "mydir", 0);
            result.Should().Be(SettingsWrapper.TestDirPlaceholder.ToLower());

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(SettingsWrapper.ThreadIdPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 4711);
            result.Should().Be("4711");

            MockXmlOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(SettingsWrapper.TestDirPlaceholder + ", " + SettingsWrapper.ThreadIdPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 4711);
            result.Should().Be("mydir, 4711");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void CatchExceptions__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.CatchExceptions).Returns((bool?)null);
            bool result = TheOptions.CatchExceptions;
            result.Should().Be(SettingsWrapper.OptionCatchExceptionsDefaultValue);

            MockXmlOptions.Setup(o => o.CatchExceptions).Returns(!SettingsWrapper.OptionCatchExceptionsDefaultValue);
            result = TheOptions.CatchExceptions;
            result.Should().Be(!SettingsWrapper.OptionCatchExceptionsDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BreakOnFailure__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.BreakOnFailure).Returns((bool?)null);
            bool result = TheOptions.BreakOnFailure;
            result.Should().Be(SettingsWrapper.OptionBreakOnFailureDefaultValue);

            MockXmlOptions.Setup(o => o.BreakOnFailure).Returns(!SettingsWrapper.OptionBreakOnFailureDefaultValue);
            result = TheOptions.BreakOnFailure;
            result.Should().Be(!SettingsWrapper.OptionBreakOnFailureDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PrintTestOutput__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.PrintTestOutput).Returns((bool?)null);
            bool result = TheOptions.PrintTestOutput;
            result.Should().Be(SettingsWrapper.OptionPrintTestOutputDefaultValue);

            MockXmlOptions.Setup(o => o.PrintTestOutput).Returns(!SettingsWrapper.OptionPrintTestOutputDefaultValue);
            result = TheOptions.PrintTestOutput;
            result.Should().Be(!SettingsWrapper.OptionPrintTestOutputDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParseSymbolInformation__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.ParseSymbolInformation).Returns((bool?)null);
            bool result = TheOptions.ParseSymbolInformation;
            result.Should().Be(SettingsWrapper.OptionParseSymbolInformationDefaultValue);

            MockXmlOptions.Setup(o => o.ParseSymbolInformation).Returns(!SettingsWrapper.OptionParseSymbolInformationDefaultValue);
            result = TheOptions.ParseSymbolInformation;
            result.Should().Be(!SettingsWrapper.OptionParseSymbolInformationDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void RunDisabledTests__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.RunDisabledTests).Returns((bool?)null);
            bool result = TheOptions.RunDisabledTests;
            result.Should().Be(SettingsWrapper.OptionRunDisabledTestsDefaultValue);

            MockXmlOptions.Setup(o => o.RunDisabledTests).Returns(!SettingsWrapper.OptionRunDisabledTestsDefaultValue);
            result = TheOptions.RunDisabledTests;
            result.Should().Be(!SettingsWrapper.OptionRunDisabledTestsDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ShuffleTests__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.ShuffleTests).Returns((bool?)null);
            bool result = TheOptions.ShuffleTests;
            result.Should().Be(SettingsWrapper.OptionShuffleTestsDefaultValue);

            MockXmlOptions.Setup(o => o.ShuffleTests).Returns(!SettingsWrapper.OptionShuffleTestsDefaultValue);
            result = TheOptions.ShuffleTests;
            result.Should().Be(!SettingsWrapper.OptionShuffleTestsDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void DebugMode__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.DebugMode).Returns((bool?)null);
            bool result = TheOptions.DebugMode;
            result.Should().Be(SettingsWrapper.OptionDebugModeDefaultValue);

            MockXmlOptions.Setup(o => o.DebugMode).Returns(!SettingsWrapper.OptionDebugModeDefaultValue);
            result = TheOptions.DebugMode;
            result.Should().Be(!SettingsWrapper.OptionDebugModeDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ParallelTestExecution__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.ParallelTestExecution).Returns((bool?)null);
            bool result = TheOptions.ParallelTestExecution;
            result.Should().Be(SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);

            MockXmlOptions.Setup(o => o.ParallelTestExecution).Returns(!SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);
            result = TheOptions.ParallelTestExecution;
            result.Should().Be(!SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void AdditionalTestDiscoveryParam__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.AdditionalTestDiscoveryParam).Returns((string)null);
            string result = TheOptions.AdditionalTestDiscoveryParam;
            result.Should().Be(SettingsWrapper.OptionAdditionalTestDiscoveryParamsDefaultValue);

            MockXmlOptions.Setup(o => o.AdditionalTestDiscoveryParam).Returns("-testDiscoveryFlag");
            result = TheOptions.AdditionalTestDiscoveryParam;
            result.Should().Be("-testDiscoveryFlag");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void TestDiscoveryRegex__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TestDiscoveryRegex).Returns((string)null);
            string result = TheOptions.TestDiscoveryRegex;
            result.Should().Be(SettingsWrapper.OptionTestDiscoveryRegexDefaultValue);

            MockXmlOptions.Setup(o => o.TestDiscoveryRegex).Returns("FooBar");
            result = TheOptions.TestDiscoveryRegex;
            result.Should().Be("FooBar");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void TestNameSeparator__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TestNameSeparator).Returns((string)null);
            string result = TheOptions.TestNameSeparator;
            result.Should().Be(SettingsWrapper.OptionTestNameSeparatorDefaultValue);

            MockXmlOptions.Setup(o => o.TestNameSeparator).Returns("FooBar");
            result = TheOptions.TestNameSeparator;
            result.Should().Be("FooBar");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void PathExtension__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.PathExtension).Returns((string)null);
            string result = TheOptions.PathExtension;
            result.Should().Be(SettingsWrapper.OptionPathExtensionDefaultValue);

            MockXmlOptions.Setup(o => o.PathExtension).Returns("FooBar");
            result = TheOptions.PathExtension;
            result.Should().Be("FooBar");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetPathExtension__PlaceholderIsReplaced()
        {
            MockXmlOptions.Setup(o => o.PathExtension).Returns("Foo;" + SettingsWrapper.ExecutableDirPlaceholder + ";Bar");
            string result = TheOptions.GetPathExtension(TestResources.Tests_DebugX86);

            // ReSharper disable once PossibleNullReferenceException
            string expectedDirectory = new FileInfo(TestResources.Tests_DebugX86).Directory.FullName;
            result.Should().Be($"Foo;{expectedDirectory};Bar");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BatchForTestTeardown__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.BatchForTestTeardown).Returns((string)null);
            string result = TheOptions.BatchForTestTeardown;
            result.Should().Be(SettingsWrapper.OptionBatchForTestTeardownDefaultValue);

            MockXmlOptions.Setup(o => o.BatchForTestTeardown).Returns("FooBar");
            result = TheOptions.BatchForTestTeardown;
            result.Should().Be("FooBar");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void BatchForTestSetup__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.BatchForTestSetup).Returns((string)null);
            string result = TheOptions.BatchForTestSetup;
            result.Should().Be(SettingsWrapper.OptionBatchForTestSetupDefaultValue);

            MockXmlOptions.Setup(o => o.BatchForTestSetup).Returns("FooBar");
            result = TheOptions.BatchForTestSetup;
            result.Should().Be("FooBar");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void TraitsRegexesBefore__ReturnsParsedValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TraitsRegexesBefore).Returns((string)null);
            List<RegexTraitPair> result = TheOptions.TraitsRegexesBefore;
            result.Should().Equal(new List<RegexTraitPair>());

            MockXmlOptions.Setup(o => o.TraitsRegexesBefore).Returns("Foo///Bar,Baz");
            result = TheOptions.TraitsRegexesBefore;
            result.Count.Should().Be(1);
            RegexTraitPair resultPair = result[0];
            resultPair.Regex.Should().Be("Foo");
            resultPair.Trait.Name.Should().Be("Bar");
            resultPair.Trait.Value.Should().Be("Baz");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void TraitsRegexesAfter__ReturnsParsedValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TraitsRegexesAfter).Returns((string)null);
            List<RegexTraitPair> result = TheOptions.TraitsRegexesAfter;
            result.Should().Equal(new List<RegexTraitPair>());

            MockXmlOptions.Setup(o => o.TraitsRegexesAfter).Returns("Foo///Bar,Baz");
            result = TheOptions.TraitsRegexesAfter;
            result.Count.Should().Be(1);
            RegexTraitPair resultPair = result[0];
            resultPair.Regex.Should().Be("Foo");
            resultPair.Trait.Name.Should().Be("Bar");
            resultPair.Trait.Value.Should().Be("Baz");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ToString_PrintsCorrectly()
        {
            MockXmlOptions.Setup(s => s.TraitsRegexesBefore).Returns("Foo///Bar,Baz//||//Foo2///Bar2,Baz2");
            MockXmlOptions.Setup(s => s.BatchForTestSetup).Returns(@"C:\\myfolder\myfile.xml");
            MockXmlOptions.Setup(s => s.MaxNrOfThreads).Returns(1);

            string optionsString = TheOptions.ToString();
            optionsString.Should().Contain("UseNewTestExecutionFramework: True");
            optionsString.Should().Contain("PrintTestOutput: False");
            optionsString.Should().Contain("TestDiscoveryRegex: ''");
            optionsString.Should().Contain("WorkingDir: '$(ExecutableDir)'");
            optionsString.Should().Contain("PathExtension: ''");
            optionsString.Should().Contain("TraitsRegexesBefore: {'Foo': (Bar,Baz), 'Foo2': (Bar2,Baz2)}");
            optionsString.Should().Contain("TraitsRegexesAfter: {}");
            optionsString.Should().Contain("TestNameSeparator: ''");
            optionsString.Should().Contain("ParseSymbolInformation: True");
            optionsString.Should().Contain("DebugMode: False");
            optionsString.Should().Contain("TimestampOutput: False");
            optionsString.Should().Contain("ShowReleaseNotes: True");
            optionsString.Should().Contain("AdditionalTestDiscoveryParam: ''");
            optionsString.Should().Contain("AdditionalTestExecutionParam: ''");
            optionsString.Should().Contain("BatchForTestSetup: 'C:\\\\myfolder\\myfile.xml'");
            optionsString.Should().Contain("BatchForTestTeardown: ''");
            optionsString.Should().Contain("ParallelTestExecution: False");
            optionsString.Should().Contain("MaxNrOfThreads: 1");
            optionsString.Should().Contain("CatchExceptions: True");
            optionsString.Should().Contain("BreakOnFailure: False");
            optionsString.Should().Contain("RunDisabledTests: False");
            optionsString.Should().Contain("NrOfTestRepetitions: 1");
            optionsString.Should().Contain("ShuffleTests: False");
            optionsString.Should().Contain("ShuffleTestsSeed: 0");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ExecuteWithSettingsForExecutable_NewInstance_ShouldDeliverSolutionSettings()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo");

            settings.WorkingDir.Should().Be("solution_dir");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ExecuteWithSettingsForExecutable_WithConfiguredProject_ShouldDeliverProjectSettings()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo");

            settings.ExecuteWithSettingsForExecutable("foo", () =>
            {
                settings.WorkingDir.Should().Be("foo_dir");
            }, MockLogger.Object);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ExecuteWithSettingsForExecutable_WithConfiguredProject_ShouldSwitchBackToSolutionSettingsAfterExecution()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo");

            settings.ExecuteWithSettingsForExecutable("foo", () => {}, MockLogger.Object);

            settings.WorkingDir.Should().Be("solution_dir");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ExecuteWithSettingsForExecutable_WithUnconfiguredProject_ShouldDeliverSolutionSettings()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo");

            settings.ExecuteWithSettingsForExecutable("bar", () =>
            {
                settings.WorkingDir.Should().Be("solution_dir");
            }, MockLogger.Object);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ExecuteWithSettingsForExecutable_NestedExecutionOfSameExecutable_ShouldDeliverProjectSettings()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo");

            settings.ExecuteWithSettingsForExecutable("foo", () =>
            {
                settings.ExecuteWithSettingsForExecutable("foo", () =>
                {
                    settings.WorkingDir.Should().Be("foo_dir");
                }, MockLogger.Object);
                
            }, MockLogger.Object);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ExecuteWithSettingsForExecutable_NestedExecutionOfDifferentExecutables_ShouldThrow()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo");

            settings
                .Invoking(s => s.ExecuteWithSettingsForExecutable("foo", () =>
                {
                    s.ExecuteWithSettingsForExecutable("bar", () => { }, MockLogger.Object);
                }, MockLogger.Object))
                .ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Clone_WhileExecuting_ReturnsFreshSettingsWrapperInstance()
        {
            var settings = CreateSettingsWrapper("solution_dir", "foo", "bar");

            settings.ExecuteWithSettingsForExecutable("foo", () =>
            {
                var settingsClone = settings.Clone();
                settingsClone.WorkingDir.Should().Be("solution_dir");
                settingsClone.ExecuteWithSettingsForExecutable("bar", () =>
                {
                    settings.WorkingDir.Should().Be("foo_dir");
                    settingsClone.WorkingDir.Should().Be("bar_dir");
                }, MockLogger.Object);
            }, MockLogger.Object);
        }

        private SettingsWrapper CreateSettingsWrapper(string solutionWorkdir, params string[] projects)
        {
            var containerMock = new Mock<IGoogleTestAdapterSettingsContainer>();

            var solutionRunSettings = new RunSettings { WorkingDir = solutionWorkdir };
            containerMock.Setup(c => c.SolutionSettings).Returns(solutionRunSettings);

            foreach (string project in projects)
            {
                var projectRunSettings = new RunSettings { WorkingDir = $"{project}_dir" };
                containerMock.Setup(c => c.GetSettingsForExecutable(It.Is<string>(s => s == project))).Returns(projectRunSettings);
            }

            return new SettingsWrapper(containerMock.Object)
            {
                RegexTraitParser = new RegexTraitParser(MockLogger.Object)
            };
        }

    }

}