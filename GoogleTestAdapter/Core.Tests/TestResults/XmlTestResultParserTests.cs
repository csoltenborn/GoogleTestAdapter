using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class XmlTestResultParserTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_FailingSetUpTestSuiteWestOfUtc_TestsFailAndTearDownTestSuiteTestCasePasses()
        {
            // Google Test writes the timestamp of tests which have not been started (0) in local time
            const string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuites tests=""2"" failures=""0"" disabled=""0"" errors=""0"" time=""0."" timestamp=""2026-10-13T02:43:21.000"" name=""AllTests"">
  <testsuite name=""FailingSetUp"" tests=""2"" failures=""0"" disabled=""0"" skipped=""2"" errors=""0"" time=""0."" timestamp=""2026-10-13T02:43:21.000"">
    <testcase name=""First"" file=""C:\tests\fixtures.cpp"" line=""7"" status=""run"" result=""skipped"" time=""0."" timestamp=""1969-12-31T16:00:00.000"" classname=""FailingSetUp"">
      <skipped message=""C:\tests\fixtures.cpp:7&#x0A;""><![CDATA[C:\tests\fixtures.cpp:7
]]></skipped>
    </testcase>
    <testcase name=""Second"" file=""C:\tests\fixtures.cpp"" line=""8"" status=""run"" result=""skipped"" time=""0."" timestamp=""1969-12-31T16:00:00.000"" classname=""FailingSetUp"">
      <skipped message=""C:\tests\fixtures.cpp:8&#x0A;""><![CDATA[C:\tests\fixtures.cpp:8
]]></skipped>
    </testcase>
    <testcase name="""" status=""run"" result=""completed"" classname="""" time=""0."" timestamp=""1969-12-31T16:00:00.000"">
      <failure message=""C:\tests\fixtures.cpp:5&#x0A;Failed&#x0A;setup failed&#x0A;"" type=""""><![CDATA[C:\tests\fixtures.cpp:5
Failed
setup failed
]]></failure>
    </testcase>
  </testsuite>
</testsuites>";
            string xmlFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(xmlFile, xml);
                List<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("FailingSetUp.First", "FailingSetUp.Second").ToList();
                Model.TestCase tearDownTestCase = TestDataCreator.ToTestCase("FailingSetUp.TearDownTestSuite");
                tearDownTestCase.Properties.Add(new Model.TestCaseMetaDataProperty(2, 2, isTearDownTestSuite: true));
                testCases.Add(tearDownTestCase);

                List<Model.TestResult> results = new XmlTestResultParser(testCases, "someexecutable", xmlFile, TestEnvironment.Logger).GetTestResults();

                results.Should().HaveCount(3);
                foreach (string testName in new[] { "FailingSetUp.First", "FailingSetUp.Second" })
                {
                    Model.TestResult result = results.Single(r => r.TestCase.FullyQualifiedName == testName);
                    result.Outcome.Should().Be(Model.TestOutcome.Failed, testName);
                    result.ErrorMessage.Should().StartWith("SetUpTestSuite() failed:").And.Contain("setup failed");
                }
                results.Single(r => r.TestCase.FullyQualifiedName == "FailingSetUp.TearDownTestSuite").Outcome.Should().Be(Model.TestOutcome.Passed);
            }
            finally
            {
                File.Delete(xmlFile);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_FileDoesNotExist_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest",
                "FooSuite.BazTest", "BarSuite.BazTest2");

            var parser = new XmlTestResultParser(testCases, "someexecutable", "somefile", TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().BeEmpty();
            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_InvalidFile_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.OutputMode).Returns(OutputMode.Verbose);

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFileBroken, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().BeEmpty();
            MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_FileWithInvalidStatusAttribute_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.OutputMode).Returns(OutputMode.Verbose);

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFileBroken_InvalidStatusAttibute, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsPassedAndSkipptedResults()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001", "SimpleTest.DISABLED_TestMethodDisabled");

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().HaveCount(2);
            AssertTestResultIsPassed(results[0]);
            AssertTestResultIsSkipped(results[1]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_TestSkippedWithGTestSkip_IsSkippedWithMessage()
        {
            List<Model.TestResult> results = ParseSkipResultXml(@"C:\src\Skip.cpp:2&#x0A;not supported on this machine&#x0A;",
                "C:\\src\\Skip.cpp:2\nnot supported on this machine\n");

            results.Should().HaveCount(2);
            Model.TestResult skipped = results.Single(r => r.TestCase.FullyQualifiedName == "SkipSuite.skipped");
            skipped.Outcome.Should().Be(Model.TestOutcome.Skipped);
            skipped.ErrorMessage.Should().Be("not supported on this machine");
            skipped.ErrorStackTrace.Should().Be($@"at SkipSuite.skipped in C:\src\Skip.cpp:line 2{Environment.NewLine}");
            AssertTestResultIsPassed(results.Single(r => r.TestCase.FullyQualifiedName == "SkipSuite.passing"));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_TestSkippedWithGTestSkipWithoutMessage_IsSkipped()
        {
            List<Model.TestResult> results = ParseSkipResultXml(@"C:\src\Skip.cpp:2&#x0A;", "C:\\src\\Skip.cpp:2\n");

            results.Should().HaveCount(2);
            AssertTestResultIsSkipped(results.Single(r => r.TestCase.FullyQualifiedName == "SkipSuite.skipped"));
            AssertTestResultIsPassed(results.Single(r => r.TestCase.FullyQualifiedName == "SkipSuite.passing"));
        }

        // as written by Google Test 1.18
        private List<Model.TestResult> ParseSkipResultXml(string skipMessageAttribute, string skipMessage)
        {
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuites tests=""2"" failures=""0"" disabled=""0"" errors=""0"" time=""0.002"" timestamp=""2026-10-08T11:33:52.592"" name=""AllTests"">
  <testsuite name=""SkipSuite"" tests=""2"" failures=""0"" disabled=""0"" skipped=""1"" errors=""0"" time=""0."" timestamp=""2026-10-08T11:33:52.592"">
    <testcase name=""skipped"" file=""C:\src\Skip.cpp"" line=""2"" status=""run"" result=""skipped"" time=""0."" timestamp=""2026-10-08T11:33:52.592"" classname=""SkipSuite"">
      <skipped message=""{skipMessageAttribute}""><![CDATA[{skipMessage}]]></skipped>
    </testcase>
    <testcase name=""passing"" file=""C:\src\Skip.cpp"" line=""3"" status=""run"" result=""completed"" time=""0."" timestamp=""2026-10-08T11:33:52.593"" classname=""SkipSuite"" />
  </testsuite>
</testsuites>";
            string xmlFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(xmlFile, xml, new UTF8Encoding(false));
                IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("SkipSuite.skipped", "SkipSuite.passing");
                return new XmlTestResultParser(testCases, "someexecutable", xmlFile, TestEnvironment.Logger).GetTestResults();
            }
            finally
            {
                File.Delete(xmlFile);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_UnexpectedTestOutcome_LogsErrorAndThrows()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_007");

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile1, TestEnvironment.Logger);
            parser.Invoking(p => p.GetTestResults()).Should().NotThrow<Exception>();
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Foo"))), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsPassedParameterizedResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("ParameterizedTestsTest1/AllEnabledTest.TestInstance/7");

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.ToTestCase("AnimalsTest.testGetEnoughAnimals", TestDataCreator.DummyExecutable, @"x:\prod\company\util\util.cpp").Yield();

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            string ErrorMsg = @"Value of: animals.size()
  Actual: 1
Expected: 3
Should get three animals";
            AssertTestResultIsFailure(results[0], ErrorMsg);
            results[0].ErrorStackTrace.Should().Contain(@"x:\prod\company\util\util.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsParameterizedFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.ToTestCase("ParameterizedTestsTest1/AllEnabledTest.TestInstance/11", TestDataCreator.DummyExecutable, @"someSimpleParameterizedTest.cpp").Yield();

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            string errorMsg = @"Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0";
            AssertTestResultIsFailure(results[0], errorMsg);
            results[0].ErrorStackTrace.Should().Contain(@"someSimpleParameterizedTest.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample2_FindsPassedResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("FooTest.DoesXyz");

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile2, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample2_FindsFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.ToTestCase("FooTest.MethodBarDoesAbc", TestDataCreator.DummyExecutable,
                @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp").Yield();

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlFile2, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            string ErrorMsg = @"#1 - Value of: output_filepath
  Actual: ""this/package/testdata/myoutputfile.dat""
Expected: input_filepath
Which is: ""this/package/testdata/myinputfile.dat""
Something's not right!!
#2 - Value of: 56456
Expected: 12312
Something's wrong :(";
            AssertTestResultIsFailure(results[0], ErrorMsg);
            results[0].ErrorStackTrace.Should().Contain(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Umlauts_FindsOneResultAndWarns()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("TheClass.Täst1", "TheClass.Töst1", "TheClass.Täst2");

            var parser = new XmlTestResultParser(testCases, "someexecutable", TestResources.XmlUmlauts, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Should().ContainSingle();
            AssertTestResultIsPassed(results[0]);
            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_UmlautsInAnsiEncodedFile_FindsResults()
        {
            // Google Test claims UTF-8, but writes test names as encoded in the executable
            const string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuites tests=""2"" failures=""0"" disabled=""0"" errors=""0"" time=""0."" name=""AllTests"">
  <testsuite name=""Ümlautß"" tests=""2"" failures=""0"" disabled=""0"" skipped=""0"" errors=""0"" time=""0."">
    <testcase name=""Täst"" status=""run"" result=""completed"" time=""0."" classname=""Ümlautß"" />
    <testcase name=""Test"" status=""run"" result=""completed"" time=""0.001"" classname=""Ümlautß"" />
  </testsuite>
</testsuites>";
            string xmlFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(xmlFile, Encoding.Default.GetBytes(xml));
                IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Ümlautß.Täst", "Ümlautß.Test");

                var parser = new XmlTestResultParser(testCases, "someexecutable", xmlFile, TestEnvironment.Logger);
                List<Model.TestResult> results = parser.GetTestResults();

                results.Should().HaveCount(2);
                results.ForEach(AssertTestResultIsPassed);
                MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Never);
                MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                File.Delete(xmlFile);
            }
        }


        public static void AssertTestResultIsPassed(Model.TestResult testResult)
        {
            testResult.Outcome.Should().Be(Model.TestOutcome.Passed);
            testResult.ErrorMessage.Should().BeNull();
        }

        public static void AssertTestResultIsSkipped(Model.TestResult testResult)
        {
            testResult.Outcome.Should().Be(Model.TestOutcome.Skipped);
            testResult.ErrorMessage.Should().BeNull();
        }

        private void AssertTestResultIsFailure(Model.TestResult testResult, string errorMessage)
        {
            AssertTestResultIsFailure(testResult);
            testResult.ErrorMessage.Replace("\r\n", "\n").Should().Be(errorMessage.Replace("\r\n", "\n"));
        }

        public static void AssertTestResultIsFailure(Model.TestResult testResult)
        {
            testResult.Outcome.Should().Be(Model.TestOutcome.Failed);
            testResult.ErrorMessage.Should().NotBeNull();
        }

    }

}