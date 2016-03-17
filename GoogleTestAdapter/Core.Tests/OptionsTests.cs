using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{

    [TestClass]
    public class OptionsTests : AbstractGoogleTestExtensionTests
    {

        private Mock<IXmlOptions> MockXmlOptions { get; } = new Mock<IXmlOptions>();
        private Options TheOptions { get; set; }


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
        public void NrOfTestRepitions_InvalidValue_ReturnsDefaultValue()
        {
            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(-2);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(0);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);
            Assert.AreEqual(4711, TheOptions.NrOfTestRepetitions);
        }

        [TestMethod]
        public void ShuffleTestsSeed_InvalidValue_ReturnsDefaultValue()
        {
            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(-1);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(1000000);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);
            Assert.AreEqual(4711, TheOptions.ShuffleTestsSeed);
        }

        [TestMethod]
        public void MaxNrOfThreads_InvalidValue_ReturnsDefaultValue()
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
        public void AdditionalTestExecutionParam__PlaceholdersAreTreatedCorrectly()
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
        public void CatchExceptions__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.CatchExceptions).Returns((bool?)null);
            bool result = TheOptions.CatchExceptions;
            Assert.AreEqual(Options.OptionCatchExceptionsDefaultValue, result);

            MockXmlOptions.Setup(o => o.CatchExceptions).Returns(!Options.OptionCatchExceptionsDefaultValue);
            result = TheOptions.CatchExceptions;
            Assert.AreEqual(!Options.OptionCatchExceptionsDefaultValue, result);
        }

        [TestMethod]
        public void BreakOnFailure__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.BreakOnFailure).Returns((bool?)null);
            bool result = TheOptions.BreakOnFailure;
            Assert.AreEqual(Options.OptionBreakOnFailureDefaultValue, result);

            MockXmlOptions.Setup(o => o.BreakOnFailure).Returns(!Options.OptionBreakOnFailureDefaultValue);
            result = TheOptions.BreakOnFailure;
            Assert.AreEqual(!Options.OptionBreakOnFailureDefaultValue, result);
        }

        [TestMethod]
        public void PrintTestOutput__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.PrintTestOutput).Returns((bool?)null);
            bool result = TheOptions.PrintTestOutput;
            Assert.AreEqual(Options.OptionPrintTestOutputDefaultValue, result);

            MockXmlOptions.Setup(o => o.PrintTestOutput).Returns(!Options.OptionPrintTestOutputDefaultValue);
            result = TheOptions.PrintTestOutput;
            Assert.AreEqual(!Options.OptionPrintTestOutputDefaultValue, result);
        }

        [TestMethod]
        public void RunDisabledTests__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.RunDisabledTests).Returns((bool?)null);
            bool result = TheOptions.RunDisabledTests;
            Assert.AreEqual(Options.OptionRunDisabledTestsDefaultValue, result);

            MockXmlOptions.Setup(o => o.RunDisabledTests).Returns(!Options.OptionRunDisabledTestsDefaultValue);
            result = TheOptions.RunDisabledTests;
            Assert.AreEqual(!Options.OptionRunDisabledTestsDefaultValue, result);
        }

        [TestMethod]
        public void ShuffleTests__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.ShuffleTests).Returns((bool?)null);
            bool result = TheOptions.ShuffleTests;
            Assert.AreEqual(Options.OptionShuffleTestsDefaultValue, result);

            MockXmlOptions.Setup(o => o.ShuffleTests).Returns(!Options.OptionShuffleTestsDefaultValue);
            result = TheOptions.ShuffleTests;
            Assert.AreEqual(!Options.OptionShuffleTestsDefaultValue, result);
        }

        [TestMethod]
        public void DebugMode__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.DebugMode).Returns((bool?)null);
            bool result = TheOptions.DebugMode;
            Assert.AreEqual(Options.OptionDebugModeDefaultValue, result);

            MockXmlOptions.Setup(o => o.DebugMode).Returns(!Options.OptionDebugModeDefaultValue);
            result = TheOptions.DebugMode;
            Assert.AreEqual(!Options.OptionDebugModeDefaultValue, result);
        }

        [TestMethod]
        public void ParallelTestExecution__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.ParallelTestExecution).Returns((bool?)null);
            bool result = TheOptions.ParallelTestExecution;
            Assert.AreEqual(Options.OptionEnableParallelTestExecutionDefaultValue, result);

            MockXmlOptions.Setup(o => o.ParallelTestExecution).Returns(!Options.OptionEnableParallelTestExecutionDefaultValue);
            result = TheOptions.ParallelTestExecution;
            Assert.AreEqual(!Options.OptionEnableParallelTestExecutionDefaultValue, result);
        }

        [TestMethod]
        public void TestDiscoveryRegex__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TestDiscoveryRegex).Returns((string)null);
            string result = TheOptions.TestDiscoveryRegex;
            Assert.AreEqual(Options.OptionTestDiscoveryRegexDefaultValue, result);

            MockXmlOptions.Setup(o => o.TestDiscoveryRegex).Returns("FooBar");
            result = TheOptions.TestDiscoveryRegex;
            Assert.AreEqual("FooBar", result);
        }

        [TestMethod]
        public void TestNameSeparator__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TestNameSeparator).Returns((string)null);
            string result = TheOptions.TestNameSeparator;
            Assert.AreEqual(Options.OptionTestNameSeparatorDefaultValue, result);

            MockXmlOptions.Setup(o => o.TestNameSeparator).Returns("FooBar");
            result = TheOptions.TestNameSeparator;
            Assert.AreEqual("FooBar", result);
        }

        [TestMethod]
        public void PathExtension__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.PathExtension).Returns((string)null);
            string result = TheOptions.PathExtension;
            Assert.AreEqual(Options.OptionPathExtensionDefaultValue, result);

            MockXmlOptions.Setup(o => o.PathExtension).Returns("FooBar");
            result = TheOptions.PathExtension;
            Assert.AreEqual("FooBar", result);
        }

        [TestMethod]
        public void BatchForTestTeardown__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.BatchForTestTeardown).Returns((string)null);
            string result = TheOptions.BatchForTestTeardown;
            Assert.AreEqual(Options.OptionBatchForTestTeardownDefaultValue, result);

            MockXmlOptions.Setup(o => o.BatchForTestTeardown).Returns("FooBar");
            result = TheOptions.BatchForTestTeardown;
            Assert.AreEqual("FooBar", result);
        }

        [TestMethod]
        public void BatchForTestSetup__ReturnsValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.BatchForTestSetup).Returns((string)null);
            string result = TheOptions.BatchForTestSetup;
            Assert.AreEqual(Options.OptionBatchForTestSetupDefaultValue, result);

            MockXmlOptions.Setup(o => o.BatchForTestSetup).Returns("FooBar");
            result = TheOptions.BatchForTestSetup;
            Assert.AreEqual("FooBar", result);
        }

        [TestMethod]
        public void TraitsRegexesBefore__ReturnsParsedValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TraitsRegexesBefore).Returns((string)null);
            List<RegexTraitPair> result = TheOptions.TraitsRegexesBefore;
            CollectionAssert.AreEqual(new List<RegexTraitPair>(), result);

            MockXmlOptions.Setup(o => o.TraitsRegexesBefore).Returns("Foo///Bar,Baz");
            result = TheOptions.TraitsRegexesBefore;
            Assert.AreEqual(1, result.Count);
            RegexTraitPair resultPair = result[0];
            Assert.AreEqual("Foo", resultPair.Regex);
            Assert.AreEqual("Bar", resultPair.Trait.Name);
            Assert.AreEqual("Baz", resultPair.Trait.Value);
        }

        [TestMethod]
        public void TraitsRegexesAfter__ReturnsParsedValueOrDefault()
        {
            MockXmlOptions.Setup(o => o.TraitsRegexesAfter).Returns((string)null);
            List<RegexTraitPair> result = TheOptions.TraitsRegexesAfter;
            CollectionAssert.AreEqual(new List<RegexTraitPair>(), result);

            MockXmlOptions.Setup(o => o.TraitsRegexesAfter).Returns("Foo///Bar,Baz");
            result = TheOptions.TraitsRegexesAfter;
            Assert.AreEqual(1, result.Count);
            RegexTraitPair resultPair = result[0];
            Assert.AreEqual("Foo", resultPair.Regex);
            Assert.AreEqual("Bar", resultPair.Trait.Name);
            Assert.AreEqual("Baz", resultPair.Trait.Value);
        }

    }

}