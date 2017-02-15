using System;
using System.IO;
using FluentAssertions;
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
            TheOptions = new SettingsWrapper(containerMock.Object);
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
            string result = TheOptions.GetPathExtension(TestResources.SampleTests);

            // ReSharper disable once PossibleNullReferenceException
            string expectedDirectory = new FileInfo(TestResources.SampleTests).Directory.FullName;
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
        public void ToString_PrintsCorrectly()
        {
            MockXmlOptions.Setup(s => s.BatchForTestSetup).Returns(@"C:\\myfolder\myfile.xml");
            MockXmlOptions.Setup(s => s.MaxNrOfThreads).Returns(1);

            string optionsString = TheOptions.ToString();
            optionsString.Should().Contain("UseNewTestExecutionFramework: True");
            optionsString.Should().Contain("PrintTestOutput: False");
            optionsString.Should().Contain("TestDiscoveryRegex: ''");
            optionsString.Should().Contain("WorkingDir: '$(ExecutableDir)'");
            optionsString.Should().Contain("PathExtension: ''");
            optionsString.Should().Contain("TestNameSeparator: ''");
            optionsString.Should().Contain("ParseSymbolInformation: True");
            optionsString.Should().Contain("DebugMode: False");
            optionsString.Should().Contain("TimestampOutput: False");
            optionsString.Should().Contain("ShowReleaseNotes: True");
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

    }

}