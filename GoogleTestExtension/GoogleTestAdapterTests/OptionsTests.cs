using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{
    [TestClass]
    public class OptionsTests : AbstractGoogleTestExtensionTests
    {

        private Mock<IXmlOptions> MockXmlOptions { get; } = new Mock<IXmlOptions>();
        private AbstractOptions TheOptions { get; set; }


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
        public void NrOfTestRepitionsHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(-2);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(0);
            Assert.AreEqual(Options.OptionNrOfTestRepetitionsDefaultValue, TheOptions.NrOfTestRepetitions);

            MockXmlOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);
            Assert.AreEqual(4711, TheOptions.NrOfTestRepetitions);
        }

        [TestMethod]
        public void ShuffleTestsSeedHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(-1);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(1000000);
            Assert.AreEqual(Options.OptionShuffleTestsSeedDefaultValue, TheOptions.ShuffleTestsSeed);

            MockXmlOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);
            Assert.AreEqual(4711, TheOptions.ShuffleTestsSeed);
        }

        [TestMethod]
        public void MaxNrOfThreadsHandlesInvalidValuesCorrectly()
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
        public void ReportWaitPeriodHandlesInvalidValuesCorrectly()
        {
            MockXmlOptions.Setup(o => o.ReportWaitPeriod).Returns(-1);
            Assert.AreEqual(Options.OptionReportWaitPeriodDefaultValue, TheOptions.ReportWaitPeriod);

            MockXmlOptions.Setup(o => o.ReportWaitPeriod).Returns(4711);
            Assert.AreEqual(4711, TheOptions.ReportWaitPeriod);
        }

        [TestMethod]
        public void AdditionalTestParameter_PlaceholdersAreTreatedCorrectly()
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

    }

}