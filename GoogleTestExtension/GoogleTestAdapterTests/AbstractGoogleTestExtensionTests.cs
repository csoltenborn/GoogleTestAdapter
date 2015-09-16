
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
        protected static string DummyExecutable { get; } = "ff.exe";

        protected readonly Mock<IMessageLogger> MockLogger = new Mock<IMessageLogger>();
        protected readonly Mock<AbstractOptions> MockOptions = new Mock<AbstractOptions>() { CallBase = true };

        internal AbstractGoogleTestExtensionTests()
        {
            Constants.UnitTestMode = true;
        }

        [TestInitialize]
        virtual public void SetUp()
        {
            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("");
        }

        [TestCleanup]
        virtual public void TearDown()
        {
            MockLogger.Reset();
            MockOptions.Reset();
        }

        protected static TestCase ToTestCase(string name)
        {
            return new TestCase(name, new Uri("http://none"), DummyExecutable);
        }

    }

}