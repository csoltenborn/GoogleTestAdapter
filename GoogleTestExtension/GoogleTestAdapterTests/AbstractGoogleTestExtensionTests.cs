
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
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
        protected readonly Mock<IRunContext> MockRunContext = new Mock<IRunContext>();
        protected readonly Mock<IFrameworkHandle> MockFrameworkHandle = new Mock<IFrameworkHandle>();

        private List<TestCase> _AllTestCasesOfConsoleApplication1 = null;
        protected List<TestCase> AllTestCasesOfConsoleApplication1 {
            get
            {
                if (_AllTestCasesOfConsoleApplication1 == null)
                {
                    _AllTestCasesOfConsoleApplication1 = new List<TestCase>();
                    GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
                    _AllTestCasesOfConsoleApplication1.AddRange(discoverer.GetTestsFromExecutable(MockLogger.Object, GoogleTestDiscovererTests.X86TraitsTests));
                    _AllTestCasesOfConsoleApplication1.AddRange(discoverer.GetTestsFromExecutable(MockLogger.Object, GoogleTestDiscovererTests.X86HardcrashingTests));
                }
                return _AllTestCasesOfConsoleApplication1;
            }
        }

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
            MockRunContext.Reset();
            MockFrameworkHandle.Reset();
            _AllTestCasesOfConsoleApplication1 = null;
        }

        protected List<TestCase> GetTestCasesOfConsoleApplication1(params string[] qualifiedNames)
        {
            List<TestCase> result = new List<TestCase>();
            foreach (TestCase testCase in AllTestCasesOfConsoleApplication1)
            {
                foreach (string qualifiedName in qualifiedNames)
                {
                    if (testCase.FullyQualifiedName.Contains(qualifiedName))
                    {
                        result.Add(testCase);
                        break;
                    }
                }
            }
            return result;
        }

        protected static TestCase ToTestCase(string name)
        {
            return new TestCase(name, new Uri("http://none"), DummyExecutable);
        }

    }

}