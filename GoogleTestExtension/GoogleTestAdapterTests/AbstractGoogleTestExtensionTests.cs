
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GoogleTestAdapter
{
    public class AbstractGoogleTestExtensionTests
    {
        protected const string EXECUTABLE = "ff.exe";

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
            MockOptions.Reset();

            MockOptions.Setup(O => O.TraitsRegexesBefore).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(O => O.TraitsRegexesAfter).Returns(new List<RegexTraitPair>());
        }

        protected static TestCase ToTestCase(string name)
        {
            return new TestCase(name, new Uri("http://none"), EXECUTABLE);
        }

    }

}