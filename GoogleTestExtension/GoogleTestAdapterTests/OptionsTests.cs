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

        private Mock<IRegistryReader> MockRegistryReader { get; } = new Mock<IRegistryReader>();

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockRegistryReader.Reset();
        }

        [TestMethod]
        public void NrOfTestRepitionsHandlesInvalidValuesCorrectly()
        {
            Options options = new Options(MockRegistryReader.Object);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(-2);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, options.NrOfTestRepetitions);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(0);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, options.NrOfTestRepetitions);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(4711);
            Assert.AreEqual(4711, options.NrOfTestRepetitions);
        }

        [TestMethod]
        public void ShuffleTestsSeedHandlesInvalidValuesCorrectly()
        {
            Options options = new Options(MockRegistryReader.Object);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(-1);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, options.ShuffleTestsSeed);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(1000000);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, options.ShuffleTestsSeed);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(4711);
            Assert.AreEqual(4711, options.ShuffleTestsSeed);
        }

        [TestMethod]
        public void MaxNrOfThreadsHandlesInvalidValuesCorrectly()
        {
            Options options = new Options(MockRegistryReader.Object);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(-1);
            Assert.AreEqual(Environment.ProcessorCount, options.MaxNrOfThreads);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Environment.ProcessorCount + 1);
            Assert.AreEqual(Environment.ProcessorCount, options.MaxNrOfThreads);

            if (Environment.ProcessorCount > 1)
            {
                MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                    .Returns(Environment.ProcessorCount - 1);
                Assert.AreEqual(Environment.ProcessorCount - 1, options.MaxNrOfThreads);
            }
        }

        [TestMethod]
        public void ReportWaitPeriodHandlesInvalidValuesCorrectly()
        {
            Options options = new Options(MockRegistryReader.Object);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(-1);
            Assert.AreEqual(Options.OptionReportWaitPeriodDefaultValue, options.ReportWaitPeriod);

            MockRegistryReader.Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(4711);
            Assert.AreEqual(4711, options.ReportWaitPeriod);
        }

        [TestMethod]
        public void AdditionalTestParameter_PlaceholdersAreTreatedCorrectly()
        {
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder);
            string result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder + " " + Options.TestDirPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir mydir", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder.ToLower());
            result = MockOptions.Object.GetUserParameters("", "mydir", 0);
            Assert.AreEqual(Options.TestDirPlaceholder.ToLower(), result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.ThreadIdPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("4711", result);

            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.TestDirPlaceholder + ", " + Options.ThreadIdPlaceholder);
            result = MockOptions.Object.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("mydir, 4711", result);
        }

        [TestMethod]
        public void TraitsRegexOptionsFailsNicelyIfInvokedWithUnparsableString()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", "vrr<erfwe") as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfEmpty()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", "") as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfOne()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            string optionsString = CreateTraitsRegex("MyTest*", "Type", "Small");
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", optionsString) as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("MyTest*", result[0].Regex);
            Assert.AreEqual("Type", result[0].Trait.Name);
            Assert.AreEqual("Small", result[0].Trait.Value);
        }

        [TestMethod]
        public void TraitsRegexOptionsAreParsedCorrectlyIfTwo()
        {
            PrivateObject optionsAccessor = new PrivateObject(new Options());
            string optionsString = ConcatTraisRegexes(
                CreateTraitsRegex("MyTest*", "Type", "Small"),
                CreateTraitsRegex("*MyOtherTest*", "Category", "Integration"));
            List<RegexTraitPair> result = optionsAccessor.Invoke("ParseTraitsRegexesString", optionsString) as List<RegexTraitPair>;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);

            Assert.AreEqual("MyTest*", result[0].Regex);
            Assert.AreEqual("Type", result[0].Trait.Name);
            Assert.AreEqual("Small", result[0].Trait.Value);

            Assert.AreEqual("*MyOtherTest*", result[1].Regex);
            Assert.AreEqual("Category", result[1].Trait.Name);
            Assert.AreEqual("Integration", result[1].Trait.Value);
        }

        private string CreateTraitsRegex(string regex, string name, string value)
        {
            return regex +
                Options.TraitsRegexesRegexSeparator + name +
                Options.TraitsRegexesTraitSeparator + value;
        }

        private string ConcatTraisRegexes(params string[] regexes)
        {
            return string.Join(Options.TraitsRegexesPairSeparator, regexes);
        }

    }

}