using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Settings
{

    [TestClass]
    public class SettingsWrapperTests : AbstractCoreTests
    {

        private Mock<IGoogleTestAdapterSettings> MockXmlOptions { get; } = new Mock<IGoogleTestAdapterSettings>();
        private SettingsWrapper TheOptions { get; set; }


        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            TheOptions = new SettingsWrapper(MockXmlOptions.Object)
            {
                RegexTraitParser = new RegexTraitParser(TestEnvironment)
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

            MockXmlOptions.Setup(o => o.MaxNrOfThreads).Returns(Environment.ProcessorCount + 1);
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

            TheOptions.ToString().Should().Be(
                @"SettingsWrapper(PrintTestOutput: False, TestDiscoveryRegex: '', PathExtension: '', " +
                @"TraitsRegexesBefore: {'Foo': (Bar,Baz), 'Foo2': (Bar2,Baz2)}, TraitsRegexesAfter: {}, " + 
                @"TestNameSeparator: '', ParseSymbolInformation: True, DebugMode: False, " + 
                @"TimestampOutput: False, ShowReleaseNotes: True, AdditionalTestExecutionParam: '', " +
                @"BatchForTestSetup: 'C:\\myfolder\myfile.xml', " + 
                @"BatchForTestTeardown: '', ParallelTestExecution: False, MaxNrOfThreads: 1, " + 
                @"CatchExceptions: True, BreakOnFailure: False, RunDisabledTests: False, " +
                @"NrOfTestRepetitions: 1, ShuffleTests: False, ShuffleTestsSeed: 0)");
        }

    }

}