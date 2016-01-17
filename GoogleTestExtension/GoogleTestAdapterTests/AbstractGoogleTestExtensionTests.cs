using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter
{
    public abstract class AbstractGoogleTestExtensionTests
    {
#if DEBUG
        private const string BuildConfig = "Debug";
#else
        private const string BuildConfig = "Release";
#endif

        protected const string SampleTestsSolutionDir = @"..\..\..\..\SampleTests\";

        private const string TestdataDir = @"..\..\..\GoogleTestAdapterTests\bin\" + BuildConfig + @"\Resources\TestData\";

        protected const string Results0Batch = @"Tests\Returns0.bat";
        protected const string Results1Batch = @"Tests\Returns1.bat";
        protected const string SampleTests = SampleTestsSolutionDir + @"Debug\Tests.exe";
        protected const string HardCrashingSampleTests = SampleTestsSolutionDir + @"Debug\CrashingTests.exe";

        private const string X86Dir = TestdataDir + @"_x86\";
        protected const string X86StaticallyLinkedTests = X86Dir + @"StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        protected const string X86ExternallyLinkedTests = X86Dir + @"ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        protected const string X86ExternallyLinkedTestsDll = X86Dir + @"ExternallyLinkedGoogleTests\ExternalGoogleTestLibrary.dll";
        protected const string X86CrashingTests = X86Dir + @"CrashingGoogleTests\CrashingGoogleTests.exe";

        private const string X64Dir = TestdataDir + @"_x64\";
        protected const string X64StaticallyLinkedTests = X64Dir + @"StaticallyLinkedGoogleTests\StaticallyLinkedGoogleTests.exe";
        protected const string X64ExternallyLinkedTests = X64Dir + @"ExternallyLinkedGoogleTests\ExternallyLinkedGoogleTests.exe";
        protected const string X64CrashingTests = X64Dir + @"CrashingGoogleTests\CrashingGoogleTests.exe";

        protected const string XmlFile1 = TestdataDir + @"SampleResult1.xml";
        protected const string XmlFile2 = TestdataDir + @"SampleResult2.xml";
        protected const string XmlFileBroken = TestdataDir + @"SampleResult1_Broken.xml";
        protected const string XmlFileBroken_InvalidStatusAttibute = TestdataDir + @"SampleResult1 _Broken_InvalidStatusAttribute.xml";

        protected const string SolutionTestSettings = TestdataDir + @"RunSettingsServiceTests\Solution" + GoogleTestConstants.SettingsExtension;
        protected const string UserTestSettings = TestdataDir + @"RunSettingsServiceTests\User.runsettings";
        protected const string UserTestSettingsWithoutRunSettingsNode = TestdataDir + @"RunSettingsServiceTests\User_WithoutRunSettingsNode.runsettings";

        protected const string DummyExecutable = "ff.exe";


        protected readonly Mock<ILogger> MockLogger = new Mock<ILogger>();
        protected readonly Mock<Options> MockOptions = new Mock<Options>() { CallBase = true };
        protected readonly Mock<ITestFrameworkReporter> MockFrameworkReporter = new Mock<ITestFrameworkReporter>();
        protected readonly TestEnvironment TestEnvironment;

        private List<TestCase> _allTestCasesOfSampleTests = null;
        protected List<TestCase> AllTestCasesOfSampleTests
        {
            get
            {
                if (_allTestCasesOfSampleTests == null)
                {
                    _allTestCasesOfSampleTests = new List<TestCase>();
                    GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(TestEnvironment);
                    _allTestCasesOfSampleTests.AddRange(discoverer.GetTestsFromExecutable(SampleTests));
                    _allTestCasesOfSampleTests.AddRange(discoverer.GetTestsFromExecutable(HardCrashingSampleTests));
                }
                return _allTestCasesOfSampleTests;
            }
        }



        protected AbstractGoogleTestExtensionTests()
        {
            TestEnvironment = new TestEnvironment(MockOptions.Object, MockLogger.Object);
        }


        [TestInitialize]
        virtual public void SetUp()
        {
            MockOptions.Setup(o => o.ReportWaitPeriod).Returns(1);

            MockOptions.Setup(o => o.TraitsRegexesBefore).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.TraitsRegexesAfter).Returns(new List<RegexTraitPair>());
            MockOptions.Setup(o => o.NrOfTestRepetitions).Returns(Options.OptionNrOfTestRepetitionsDefaultValue);
            MockOptions.Setup(o => o.PrintTestOutput).Returns(Options.OptionPrintTestOutputDefaultValue);
            MockOptions.Setup(o => o.RunDisabledTests).Returns(Options.OptionRunDisabledTestsDefaultValue);
            MockOptions.Setup(o => o.ShuffleTests).Returns(Options.OptionShuffleTestsDefaultValue);
            MockOptions.Setup(o => o.ShuffleTestsSeed).Returns(Options.OptionShuffleTestsSeedDefaultValue);
            MockOptions.Setup(o => o.DebugMode).Returns(Options.OptionDebugModeDefaultValue);
            MockOptions.Setup(o => o.DevelopmentMode).Returns(Options.OptionDevelopmentModeDefaultValue);
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns(Options.OptionAdditionalTestExecutionParamsDefaultValue);
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(Options.OptionBatchForTestSetupDefaultValue);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(Options.OptionBatchForTestTeardownDefaultValue);
            MockOptions.Setup(o => o.ParallelTestExecution).Returns(Options.OptionEnableParallelTestExecutionDefaultValue);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(Options.OptionMaxNrOfThreadsDefaultValue);
        }

        [TestCleanup]
        virtual public void TearDown()
        {
            MockLogger.Reset();
            MockOptions.Reset();
            MockFrameworkReporter.Reset();
            _allTestCasesOfSampleTests = null;
        }

        protected List<TestCase> GetTestCasesOfSampleTests(params string[] qualifiedNames)
        {
            return AllTestCasesOfSampleTests.Where(
                testCase => qualifiedNames.Any(
                    qualifiedName => testCase.FullyQualifiedName.Contains(qualifiedName)))
                    .ToList();
        }

        protected static TestCase ToTestCase(string name, string executable)
        {
            return new TestCase(name, executable, name, "", 0);
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