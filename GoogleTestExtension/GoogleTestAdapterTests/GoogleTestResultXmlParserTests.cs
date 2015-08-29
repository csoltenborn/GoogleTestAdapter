using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestResultXmlParserTests : AbstractGoogleTestExtensionTests
    {

        private const string XML_FILE_1 = @"..\..\..\testdata\SampleResult1.xml";
        private const string XML_FILE_2 = @"..\..\..\testdata\SampleResult2.xml";
        private const string XML_FILE_BROKEN = @"..\..\..\testdata\SampleResult1_Broken.xml";

        [TestMethod]
        public void FailsNicelyIfFileDoesNotExist()
        {
            string[] Tests = new string[] { "BarSuite.BazTest1", "FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest2" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser("somefile", TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(0, Results.Count);
            MockLogger.Verify(L => L.SendMessage(It.Is<TestMessageLevel>(Tml => Tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FailsNicelyIfFileIsInvalid()
        {
            string[] Tests = new string[] { "GoogleTestSuiteName1.TestMethod_001", "GoogleTestSuiteName1.TestMethod_002" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_BROKEN, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(0, Results.Count);
            MockLogger.Verify(L => L.SendMessage(It.Is<TestMessageLevel>(Tml => Tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FindsSuccessfulResultsInSample1()
        {
            string[] Tests = new string[] { "GoogleTestSuiteName1.TestMethod_001" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_1, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            AssertTestResultIsPassed(Results[0]);
        }

        [TestMethod]
        public void FindsSuccessfulParameterizedResultInSample1()
        {
            string[] Tests = new string[] { "ParameterizedTestsTest1/AllEnabledTest.TestInstance/7  # GetParam() = (false, 200, 0)" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_1, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            AssertTestResultIsPassed(Results[0]);
        }

        [TestMethod]
        public void FindsFailureResultInSample1()
        {
            string[] Tests = new string[] { "AnimalsTest.testGetEnoughAnimals" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_1, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            string ErrorMsg = @"x:\prod\company\util\util.cpp:67
Value of: animals.size()
  Actual: 1
Expected: 3
Should get three animals";
            AssertTestResultIsFailure(Results[0], ErrorMsg);
        }

        [TestMethod]
        public void FindsParamterizedFailureResultInSample1()
        {
            string[] Tests = new string[] { "ParameterizedTestsTest1/AllEnabledTest.TestInstance/11  # GetParam() = (true, 0, 100)" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_1, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            string ErrorMsg = @"someSimpleParameterizedTest.cpp:61
Expected: (0) != ((pGSD->g_outputs64[(g_nOutput[ 8 ]-1)/64] & g_dnOutput[g_nOutput[ 8 ]])), actual: 0 vs 0";
            AssertTestResultIsFailure(Results[0], ErrorMsg);
        }

        [TestMethod]
        public void FindsSuccessfulResultInSample2()
        {
            string[] Tests = new string[] { "FooTest.DoesXyz" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_2, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            AssertTestResultIsPassed(Results[0]);
        }

        [TestMethod]
        public void FindsFailureResultInSample2()
        {
            string[] Tests = new string[] { "FooTest.MethodBarDoesAbc" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_2, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
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
            AssertTestResultIsFailure(Results[0], ErrorMsg);
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