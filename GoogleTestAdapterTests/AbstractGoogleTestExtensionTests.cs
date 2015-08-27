
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter
{
    public class AbstractGoogleTestExtensionTests
    {

        protected readonly Mock<IMessageLogger> MockLogger = new Mock<IMessageLogger>();

        internal AbstractGoogleTestExtensionTests()
        {
            Constants.UNIT_TEST_MODE = true;
        }

        [TestInitialize]
        virtual public void SetUp()
        {
            MockLogger.Reset();
        }



    }

}