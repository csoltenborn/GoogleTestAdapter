using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class XmlTestResultParserTests : AbstractGoogleTestExtensionTests
    {

        private const string XmlFile1 = @"..\..\..\testdata\SampleResult1.xml";
        private const string XmlFile2 = @"..\..\..\testdata\SampleResult2.xml";
        private const string XmlFileBroken = @"..\..\..\testdata\SampleResult1_Broken.xml";

        [TestMethod]
        public void FailsNicelyIfFileDoesNotExist()
        {
            string[] tests = { "BarSuite.BazTest1", "FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest2" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser("somefile", testCases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.SendMessage(It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FailsNicelyIfFileIsInvalid()
        {
            string[] tests = { "GoogleTestSuiteName1.TestMethod_001", "GoogleTestSuiteName1.TestMethod_002" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFileBroken, testCases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(0, results.Count);
            MockLogger.Verify(l => l.SendMessage(It.Is<TestMessageLevel>(tml => tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FindsSuccessfulResultsInSample1()
        {
            string[] tests = { "GoogleTestSuiteName1.TestMethod_001" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFile1, testCases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void FindsSuccessfulParameterizedResultInSample1()
        {
            string[] tests = { "ParameterizedTestsTest1/AllEnabledTest.TestInstance/7  # GetParam() = (false, 200, 0)" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFile1, testCases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void FindsFailureResultInSample1()
        {
            string[] tests = { "AnimalsTest.testGetEnoughAnimals" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFile1, testCases, MockLogger.Object);
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
            string[] tests = { "ParameterizedTestsTest1/AllEnabledTest.TestInstance/11  # GetParam() = (true, 0, 100)" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFile1, testCases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            string errorMsg = @"someSimpleParameterizedTest.cpp:61
Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0";
            AssertTestResultIsFailure(results[0], errorMsg);
        }

        [TestMethod]
        public void FindsSuccessfulResultInSample2()
        {
            string[] tests = { "FooTest.DoesXyz" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFile2, testCases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();

            Assert.AreEqual(1, results.Count);
            AssertTestResultIsPassed(results[0]);
        }

        [TestMethod]
        public void FindsFailureResultInSample2()
        {
            string[] tests = { "FooTest.MethodBarDoesAbc" };
            IEnumerable<TestCase> testCases = tests.Select(ToTestCase);

            XmlTestResultParser parser = new XmlTestResultParser(XmlFile2, testCases, MockLogger.Object);
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