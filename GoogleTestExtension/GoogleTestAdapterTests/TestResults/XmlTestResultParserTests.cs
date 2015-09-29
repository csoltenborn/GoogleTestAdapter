using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class XmlTestResultParserTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void FailsNicelyIfFileDoesNotExist()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest",
                "FooSuite.BazTest", "BarSuite.BazTest2");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, "somefile", TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.SendMessage(It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FailsNicelyIfFileIsInvalid()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001",
                "GoogleTestSuiteName1.TestMethod_002");
            MockOptions.Setup(o => o.UserDebugMode).Returns(true);

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFileBroken, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.SendMessage(It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FindsSuccessfulResultsInSample1()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("GoogleTestSuiteName1.TestMethod_001");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void FindsSuccessfulParameterizedResultInSample1()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("ParameterizedTestsTest1/AllEnabledTest.TestInstance/7  # GetParam() = (false, 200, 0)");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void FindsFailureResultInSample1()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("AnimalsTest.testGetEnoughAnimals");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string ErrorMsg = @"x:\prod\company\util\util.cpp:67
Value of: animals.size()
  Actual: 1
Expected: 3
Should get three animals";
            AssertTestResultIsFailure(results[0], ErrorMsg);
        }

        [TestMethod]
        public void FindsParamterizedFailureResultInSample1()
        {
            IEnumerable<TestCase> testCases =
                CreateDummyTestCases(
                    "ParameterizedTestsTest1/AllEnabledTest.TestInstance/11  # GetParam() = (true, 0, 100)");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile1, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string errorMsg = @"someSimpleParameterizedTest.cpp:61
Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0";
            AssertTestResultIsFailure(results[0], errorMsg);
        }

        [TestMethod]
        public void FindsSuccessfulResultInSample2()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("FooTest.DoesXyz");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile2, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void FindsFailureResultInSample2()
        {
            IEnumerable<TestCase> testCases = CreateDummyTestCases("FooTest.MethodBarDoesAbc");

            XmlTestResultParser parser = new XmlTestResultParser(testCases, XmlFile2, TestEnvironment);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string ErrorMsg = @"c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp:40
Value of: output_filepath
  Actual: ""this/package/testdata/myoutputfile.dat""
Expected: input_filepath
Which is: ""this/package/testdata/myinputfile.dat""
Something's not right!!

c:\prod\gtest-1.7.0\staticallylinkedgoogletests\main.cpp:41
Value of: 56456
Expected: 12312
Something's wrong :(";
            AssertTestResultIsFailure(results[0], ErrorMsg);
        }


        private void AssertTestResultIsPassed(TestResult testResult)
        {
            Assert.AreEqual(TestOutcome.Passed, testResult.Outcome);
            Assert.IsNull(testResult.ErrorMessage);
        }

        private void AssertTestResultIsFailure(TestResult testResult, string errorMessage)
        {
            Assert.AreEqual(TestOutcome.Failed, testResult.Outcome);
            Assert.IsNotNull(testResult.ErrorMessage);
            Assert.AreEqual(errorMessage.Replace("\r\n", "\n"), testResult.ErrorMessage.Replace("\r\n", "\n"));
        }

    }

}