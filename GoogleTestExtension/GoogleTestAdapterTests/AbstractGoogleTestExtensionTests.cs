using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter
{
    public class AbstractGoogleTestExtensionTests
    {
        protected const string X86StaticallyLinkedTests = @"..\..\..\testdata\_x86\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        protected const string X86ExternallyLinkedTests = @"..\..\..\testdata\_x86\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        protected const string X86CrashingTests = @"..\..\..\testdata\_x86\CrashingGoogleTests\CrashingGoogleTests.exe";
        protected const string X64StaticallyLinkedTests = @"..\..\..\testdata\_x64\StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        protected const string X64ExternallyLinkedTests = @"..\..\..\testdata\_x64\ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        protected const string X64CrashingTests = @"..\..\..\testdata\_x64\CrashingGoogleTests\CrashingGoogleTests.exe";
        protected const string X86TraitsTests = @"..\..\..\..\ConsoleApplication1\Debug\ConsoleApplication1Tests.exe";
        protected const string X86HardcrashingTests = @"..\..\..\..\ConsoleApplication1\Debug\ConsoleApplication1CrashingTests.exe";

        protected const string DummyExecutable = "ff.exe";

        protected readonly Mock<IMessageLogger> MockLogger = new Mock<IMessageLogger>();
        protected readonly Mock<AbstractOptions> MockOptions = new Mock<AbstractOptions>() { CallBase = true };
        protected readonly Mock<IRunContext> MockRunContext = new Mock<IRunContext>();
        protected readonly Mock<IFrameworkHandle> MockFrameworkHandle = new Mock<IFrameworkHandle>();

        private List<TestCase> _allTestCasesOfConsoleApplication1 = null;
        protected List<TestCase> AllTestCasesOfConsoleApplication1 {
            get
            {
                if (_allTestCasesOfConsoleApplication1 == null)
                {
                    _allTestCasesOfConsoleApplication1 = new List<TestCase>();
                    GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(MockOptions.Object);
                    _allTestCasesOfConsoleApplication1.AddRange(discoverer.GetTestsFromExecutable(MockLogger.Object, X86TraitsTests));
                    _allTestCasesOfConsoleApplication1.AddRange(discoverer.GetTestsFromExecutable(MockLogger.Object, X86HardcrashingTests));
                }
                return _allTestCasesOfConsoleApplication1;
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
        }

        [TestCleanup]
        virtual public void TearDown()
        {
            MockLogger.Reset();
            MockOptions.Reset();
            MockRunContext.Reset();
            MockFrameworkHandle.Reset();
            _allTestCasesOfConsoleApplication1 = null;
        }

        protected List<TestCase> GetTestCasesOfConsoleApplication1(params string[] qualifiedNames)
        {
            return AllTestCasesOfConsoleApplication1.Where(
                testCase => qualifiedNames.Any(
                    qualifiedName => testCase.FullyQualifiedName.Contains(qualifiedName)))
                    .ToList();
        }

        protected static TestCase ToTestCase(string name, string executable)
        {
            return new TestCase(name, new Uri("http://none"), executable);
        }

        protected static TestCase ToTestCase(string name)
        {
            return ToTestCase(name, DummyExecutable);
        }

        protected static TestResult ToTestResult(string qualifiedTestCaseName, TestOutcome outcome, int duration, string executable = DummyExecutable)
        {
            return new TestResult(ToTestCase(qualifiedTestCaseName, executable))
            {
                Outcome = outcome,
                Duration = TimeSpan.FromMilliseconds(duration)
            };
        }

        protected static List<TestCase> CreateDummyTestCases(params string[] qualifiedNames)
        {
            return qualifiedNames.Select(ToTestCase).ToList();
        }

    }

}