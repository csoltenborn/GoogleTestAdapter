using System;
using System.Collections.Generic;
using FluentAssertions;
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
        public void GetTestResults_FileDoesNotExist_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest",
                "FooSuite.BazTest", "BarSuite.BazTest2");

            var parser = new XmlTestResultParser(testCases, "somefile", TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(0);
            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_InvalidFile_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFileBroken, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(0);
            MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_FileWithInvalidStatusAttribute_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFileBroken_InvalidStatusAttibute, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(1);
            MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsPassedAndSkipptedResults()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001", "SimpleTest.DISABLED_TestMethodDisabled");

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(2);
            AssertTestResultIsPassed(results[0]);
            AssertTestResultIsSkipped(results[1]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_UnexpectedTestOutcome_LogsErrorAndThrows()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_007");

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile1, TestEnvironment.Logger);
            parser.Invoking(p => p.GetTestResults()).Should().NotThrow<Exception>();
            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Foo"))), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsPassedParameterizedResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("ParameterizedTestsTest1/AllEnabledTest.TestInstance/7");

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(1);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.ToTestCase("AnimalsTest.testGetEnoughAnimals", TestDataCreator.DummyExecutable, @"x:\prod\company\util\util.cpp").Yield();

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(1);
            string ErrorMsg = @"Value of: animals.size()
  Actual: 1
Expected: 3
Should get three animals";
            AssertTestResultIsFailure(results[0], ErrorMsg);
            results[0].ErrorStackTrace.Should().Contain(@"x:\prod\company\util\util.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample1_FindsParamterizedFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.ToTestCase("ParameterizedTestsTest1/AllEnabledTest.TestInstance/11", TestDataCreator.DummyExecutable, @"someSimpleParameterizedTest.cpp").Yield();

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile1, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(1);
            string errorMsg = @"Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0";
            AssertTestResultIsFailure(results[0], errorMsg);
            results[0].ErrorStackTrace.Should().Contain(@"someSimpleParameterizedTest.cpp");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample2_FindsPassedResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("FooTest.DoesXyz");

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile2, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(1);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_Sample2_FindsFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.ToTestCase("FooTest.MethodBarDoesAbc", TestDataCreator.DummyExecutable,
                @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp").Yield();

            var parser = new XmlTestResultParser(testCases, TestResources.XmlFile2, TestEnvironment.Logger);
            List<Model.TestResult> results = parser.GetTestResults();

            results.Count.Should().Be(1);
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


        public static void AssertTestResultIsPassed(Model.TestResult testResult)
        {
            testResult.Outcome.Should().Be(Model.TestOutcome.Passed);
            testResult.ErrorMessage.Should().BeNull();
        }

        private void AssertTestResultIsSkipped(Model.TestResult testResult)
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