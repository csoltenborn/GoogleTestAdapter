using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Moq;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter
{

    [TestClass]
    public class TestDiscovererTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void DiscoverTests_WithDefaultRegex_RegistersFoundTestsAtDiscoverySink()
        {
            CheckForDiscoverySinkCalls(2);
        }

        [TestMethod]
        public void DiscoverTests_WithCustomNonMatchingRegex_DoesNotFindTests()
        {
            CheckForDiscoverySinkCalls(0, "NoMatchAtAll");
        }


        private void CheckForDiscoverySinkCalls(int expectedNrOfTests, string customRegex = null)
        {
            Mock<IDiscoveryContext> mockDiscoveryContext = new Mock<IDiscoveryContext>();
            Mock<ITestCaseDiscoverySink> mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            MockOptions.Setup(o => o.TestDiscoveryRegex).Returns(() => customRegex);

            TestDiscoverer discoverer = new TestDiscoverer(TestEnvironment);
            Mock<IMessageLogger> MockVsLogger = new Mock<IMessageLogger>();
            discoverer.DiscoverTests(X86StaticallyLinkedTests.Yield(), mockDiscoveryContext.Object, MockVsLogger.Object, mockDiscoverySink.Object);

            mockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>()), Times.Exactly(expectedNrOfTests));
        }

    }

}