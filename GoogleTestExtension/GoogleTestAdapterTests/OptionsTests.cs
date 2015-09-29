using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{
    [TestClass]
    public class OptionsTests : AbstractGoogleTestExtensionTests
    {

        private Mock<IRegistryReader> MockRegistryReader { get; } = new Mock<IRegistryReader>();
        private AbstractOptions TheOptions { get; set; }


        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            TheOptions = new Options(MockRegistryReader.Object, MockLogger.Object);
        }

        [TestCleanup]
        public override void TearDown()
        {
            base.TearDown();

            MockRegistryReader.Reset();
        }


        [TestMethod]
        public void NrOfTestRepitionsHandlesInvalidValuesCorrectly()
        {
            SetupMockToReturnInt(-2);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            SetupMockToReturnInt(0);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            SetupMockToReturnInt(4711);
            Assert.AreEqual(4711, TheOptions.NrOfTestRepetitions);
        }

        [TestMethod]
        public void ShuffleTestsSeedHandlesInvalidValuesCorrectly()
        {
            SetupMockToReturnInt(-1);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            SetupMockToReturnInt(1000000);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            SetupMockToReturnInt(4711);
            Assert.AreEqual(4711, TheOptions.ShuffleTestsSeed);
        }

        [TestMethod]
        public void MaxNrOfThreadsHandlesInvalidValuesCorrectly()
        {
            SetupMockToReturnInt(-1);
            Assert.AreEqual(Environment.ProcessorCount, TheOptions.MaxNrOfThreads);

            SetupMockToReturnInt(Environment.ProcessorCount + 1);
            Assert.AreEqual(Environment.ProcessorCount, TheOptions.MaxNrOfThreads);

            if (Environment.ProcessorCount > 1)
            {
                SetupMockToReturnInt(Environment.ProcessorCount - 1);
                Assert.AreEqual(Environment.ProcessorCount - 1, TheOptions.MaxNrOfThreads);
            }
        }

        [TestMethod]
        public void ReportWaitPeriodHandlesInvalidValuesCorrectly()
        {
            SetupMockToReturnInt(-1);
            Assert.AreEqual(Options.OptionReportWaitPeriodDefaultValue, TheOptions.ReportWaitPeriod);

            SetupMockToReturnInt(4711);
            Assert.AreEqual(4711, TheOptions.ReportWaitPeriod);
        }

        [TestMethod]
        public void AdditionalTestParameter_PlaceholdersAreTreatedCorrectly()
        {
            SetupMockToReturnString(Options.TestDirPlaceholder);
            string result = TheOptions.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir", result);

            SetupMockToReturnString(Options.TestDirPlaceholder + " " + Options.TestDirPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 0);
            Assert.AreEqual("mydir mydir", result);

            SetupMockToReturnString(Options.TestDirPlaceholder.ToLower());
            result = TheOptions.GetUserParameters("", "mydir", 0);
            Assert.AreEqual(Options.TestDirPlaceholder.ToLower(), result);

            SetupMockToReturnString(Options.ThreadIdPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("4711", result);

            SetupMockToReturnString(Options.TestDirPlaceholder + ", " + Options.ThreadIdPlaceholder);
            result = TheOptions.GetUserParameters("", "mydir", 4711);
            Assert.AreEqual("mydir, 4711", result);
        }


        private void SetupMockToReturnString(string s)
        {
            MockRegistryReader
                .Setup(rr => rr.ReadString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(s);
        }

        private void SetupMockToReturnInt(int i)
        {
            MockRegistryReader
                .Setup(rr => rr.ReadInt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(i);
        }

    }

}