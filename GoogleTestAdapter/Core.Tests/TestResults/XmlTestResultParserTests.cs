using System;
using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class XmlTestResultParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void GetTestResults_FileDoesNotExist_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest",
                "FooSuite.BazTest", "BarSuite.BazTest2");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, "somefile", TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public void GetTestResults_InvalidFile_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFileBroken, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public void GetTestResults_FileWithInvalidStatusAttribute_WarningAndEmptyResult()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFileBroken_InvalidStatusAttibute, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public void GetTestResults_Sample1_FindsPassedAndSkipptedResults()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001", "SimpleTest.DISABLED_TestMethodDisabled");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(2, results.Count);
            AssertTestResultIsPassed(results[0]);
            AssertTestResultIsSkipped(results[1]);
        }

        [TestMethod]
        public void GetTestResults_Sample1_UnexpectedTestOutcome_LogsErrorAndThrows()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_007");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment, "");
            try
            {
                parser.GetTestResults();
                Assert.Fail();
            }
            catch (Exception)
            {
                // that's expected
            }

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Foo"))), Times.Exactly(1));
        }

        [TestMethod]
        public void GetTestResults_Sample1_FindsPassedParameterizedResult()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("ParameterizedTestsTest1/AllEnabledTest.TestInstance/7  # GetParam() = (false, 200, 0)");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void GetTestResults_Sample1_FindsFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = ToTestCase("AnimalsTest.testGetEnoughAnimals",
                DummyExecutable, @"x:\prod\company\util\util.cpp").Yield();

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string ErrorMsg = @"Value of: animals.size()
  Actual: 1
Expected: 3
Should get three animals";
            AssertTestResultIsFailure(results[0], ErrorMsg);
            Assert.IsTrue(results[0].ErrorStackTrace.Contains(@"x:\prod\company\util\util.cpp"));
        }

        [TestMethod]
        public void GetTestResults_Sample1_FindsParamterizedFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = ToTestCase("ParameterizedTestsTest1/AllEnabledTest.TestInstance/11  # GetParam() = (true, 0, 100)",
                DummyExecutable, @"someSimpleParameterizedTest.cpp").Yield();

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string errorMsg = @"Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0";
            AssertTestResultIsFailure(results[0], errorMsg);
            Assert.IsTrue(results[0].ErrorStackTrace.Contains(@"someSimpleParameterizedTest.cpp"));
        }

        [TestMethod]
        public void GetTestResults_Sample2_FindsPassedResult()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("FooTest.DoesXyz");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile2, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void GetTestResults_Sample2_FindsFailureResult()
        {
            IEnumerable<Model.TestCase> testCases = ToTestCase("FooTest.MethodBarDoesAbc", DummyExecutable,
                @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp").Yield();

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile2, TestEnvironment, "");
            List<Model.TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string ErrorMsg = @"#1 - Value of: output_filepath
  Actual: ""this/package/testdata/myoutputfile.dat""
Expected: input_filepath
Which is: ""this/package/testdata/myinputfile.dat""
Something's not right!!
#2 - Value of: 56456
Expected: 12312
Something's wrong :(";
            AssertTestResultIsFailure(results[0], ErrorMsg);
            Assert.IsTrue(results[0].ErrorStackTrace.Contains(@"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp"));
        }


        private void AssertTestResultIsPassed(Model.TestResult testResult)
        {
            Assert.AreEqual(Model.TestOutcome.Passed, testResult.Outcome);
            Assert.IsNull(testResult.ErrorMessage);
        }

        private void AssertTestResultIsSkipped(Model.TestResult testResult)
        {
            Assert.AreEqual(Model.TestOutcome.Skipped, testResult.Outcome);
            Assert.IsNull(testResult.ErrorMessage);
        }

        private void AssertTestResultIsFailure(Model.TestResult testResult, string errorMessage)
        {
            Assert.AreEqual(Model.TestOutcome.Failed, testResult.Outcome);
            Assert.IsNotNull(testResult.ErrorMessage);
            Assert.AreEqual(errorMessage.Replace("\r\n", "\n"), testResult.ErrorMessage.Replace("\r\n", "\n"));
        }

    }

}