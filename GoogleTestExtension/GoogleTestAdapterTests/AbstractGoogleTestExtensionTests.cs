using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter
{
    public abstract class AbstractGoogleTestExtensionTests
    {
        private const string ConsoleApplication1Dir = @"..\..\..\..\ConsoleApplication1\";
        private const string TestdataDir = @"Resources\TestData\";

        protected const string Results0Batch = ConsoleApplication1Dir + @"ConsoleApplication1Tests\Returns0.bat";
        protected const string Results1Batch = ConsoleApplication1Dir + @"ConsoleApplication1Tests\Returns1.bat";
        protected const string X86TraitsTests = ConsoleApplication1Dir + @"Debug\ConsoleApplication1Tests.exe";
        protected const string X86HardcrashingTests = ConsoleApplication1Dir + @"Debug\ConsoleApplication1CrashingTests.exe";

        private const string X86Dir = TestdataDir + @"_x86\";
        protected const string X86StaticallyLinkedTests = X86Dir + @"StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        protected const string X86ExternallyLinkedTests = X86Dir + @"ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        protected const string X86CrashingTests = X86Dir + @"CrashingGoogleTests\CrashingGoogleTests.exe";

        private const string X64Dir = TestdataDir + @"_x64\";
        protected const string X64StaticallyLinkedTests = X64Dir + @"StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        protected const string X64ExternallyLinkedTests = X64Dir + @"ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        protected const string X64CrashingTests = X64Dir + @"CrashingGoogleTests\CrashingGoogleTests.exe";

        protected const string XmlFile1 = TestdataDir + @"SampleResult1.xml";
        protected const string XmlFile2 = TestdataDir + @"SampleResult2.xml";
        protected const string XmlFileBroken = TestdataDir + @"SampleResult1_Broken.xml";

        protected const string DummyExecutable = "ff.exe";

        protected Mock<IMessageLogger> MockLogger = new Mock<IMessageLogger>();
        protected readonly Mock<AbstractOptions> MockOptions = new Mock<AbstractOptions>() { CallBase = true };
        protected readonly Mock<IRunContext> MockRunContext = new Mock<IRunContext>();
        protected readonly Mock<IFrameworkHandle> MockFrameworkHandle = new Mock<IFrameworkHandle>();
        internal readonly TestEnvironment TestEnvironment;

        public AbstractGoogleTestExtensionTests()
        {
            TestEnvironment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
        }

        private List<TestCase> _allTestCasesOfConsoleApplication1 = null;
        protected List<TestCase> AllTestCasesOfConsoleApplication1
        {
            get
            {
                if (_allTestCasesOfConsoleApplication1 == null)
                {
                    _allTestCasesOfConsoleApplication1 = new List<TestCase>();
                    GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
                    _allTestCasesOfConsoleApplication1.AddRange(discoverer.GetTestsFromExecutable(X86TraitsTests));
                    _allTestCasesOfConsoleApplication1.AddRange(discoverer.GetTestsFromExecutable(X86HardcrashingTests));
                }
                return _allTestCasesOfConsoleApplication1;
            }
        }

        [TestInitialize]
        virtual public void SetUp()
        {
            FieldInfo fieldInfo = typeof(TestEnvironment).GetField("UnitTestMode", BindingFlags.NonPublic | BindingFlags.Static);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(null, true);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.ReportWaitPeriod).Returns(1);
            MockOptions.Setup(o => o.NrOfTestRepetitions).Returns(1);
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

        protected static IEnumerable<TestCase> CreateDummyTestCases(params string[] qualifiedNames)
        {
            return qualifiedNames.Select(ToTestCase).ToList();
        }

    }

}