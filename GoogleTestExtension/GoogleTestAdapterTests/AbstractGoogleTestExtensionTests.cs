
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleTestAdapter
{
    public class AbstractGoogleTestExtensionTests
    {

        protected readonly Mock<IMessageLogger> MockLogger = new Mock<IMessageLogger>();
        protected readonly Mock<IOptions> MockOptions = new Mock<IOptions>();

        internal AbstractGoogleTestExtensionTests()
        {
            Constants.UNIT_TEST_MODE = true;
        }

        [TestInitialize]
        virtual public void SetUp()
        {
            MockLogger.Reset();
        }

        protected static TestCase ToTestCase(string name)
        {
            return new TestCase(name, new Uri("http://none"), "ff.exe");
        }

    }

}