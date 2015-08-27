using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
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

            Assert.IsTrue(Results.Count == 0);
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

            Assert.IsTrue(Results.Count == 0);
            MockLogger.Verify(L => L.SendMessage(It.Is<TestMessageLevel>(Tml => Tml == TestMessageLevel.Warning), It.IsAny<string>()),
                            Times.Exactly(1));
        }

        [TestMethod]
        public void FindsSuccessfulResultsFromSample1()
        {
            string[] Tests = new string[] { "GoogleTestSuiteName1.TestMethod_001" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_1, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            Assert.IsTrue(Results[0].Outcome == TestOutcome.Passed);
        }

        [TestMethod]
        public void FindsSuccessfulParameterizedResultFromSample1()
        {
            string[] Tests = new string[] { "ParameterizedTestsTest1/AllEnabledTest.TestInstance/7  # GetParam() = (false, 200, 0)" };
            IEnumerable<TestCase> TestCases = Tests.Select(ToTestCase);

            GoogleTestResultXmlParser Parser = new GoogleTestResultXmlParser(XML_FILE_1, TestCases, MockLogger.Object);
            List<TestResult> Results = Parser.GetTestResults();

            Assert.AreEqual(1, Results.Count);
            Assert.IsTrue(Results[0].Outcome == TestOutcome.Passed);
            Assert.IsTrue(Results[0].ErrorMessage == null);
        }

    }

}