using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{

    [TestClass]
    public class TestDiscovererTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_WithDefaultRegex_RegistersFoundTestsAtDiscoverySink()
        {
            CheckForDiscoverySinkCalls(2);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_WithCustomNonMatchingRegex_DoesNotFindTests()
        {
            CheckForDiscoverySinkCalls(0, "NoMatchAtAll");
        }


        private void CheckForDiscoverySinkCalls(int expectedNrOfTests, string customRegex = null)
        {
            Mock<IDiscoveryContext> mockDiscoveryContext = new Mock<IDiscoveryContext>();
            Mock<ITestCaseDiscoverySink> mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            MockOptions.Setup(o => o.TestDiscoveryRegex).Returns(() => customRegex);

            TestDiscoverer discoverer = new TestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            Mock<IMessageLogger> mockVsLogger = new Mock<IMessageLogger>();
            discoverer.DiscoverTests(TestResources.X86StaticallyLinkedTests.Yield(), mockDiscoveryContext.Object, mockVsLogger.Object, mockDiscoverySink.Object);

            mockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>()), Times.Exactly(expectedNrOfTests));
        }

    }

}