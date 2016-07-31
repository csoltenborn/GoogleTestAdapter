using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class XmlTestResultParser
    {
        private const string ErrorMsgNoXmlFile = "Output file does not exist, did your tests crash?";

        private static readonly NumberFormatInfo NumberFormatInfo = new CultureInfo("en-US").NumberFormat;


        private readonly TestEnvironment _testEnvironment;
        private readonly string _baseDir;
        private readonly string _xmlResultFile;
        private readonly IDictionary<string, TestCase> _testCasesMap;


        public XmlTestResultParser(IEnumerable<TestCase> testCasesRun, string xmlResultFile, TestEnvironment testEnvironment, string baseDir)
        {
            _testEnvironment = testEnvironment;
            _baseDir = baseDir;
            _xmlResultFile = xmlResultFile;
            _testCasesMap = testCasesRun.ToDictionary(tc => tc.FullyQualifiedName, tc => tc);
        }


        public List<TestResult> GetTestResults()
        {
            if (File.Exists(_xmlResultFile))
            {
                return ParseTestResults();
            }

            _testEnvironment.LogWarning(ErrorMsgNoXmlFile);
            return new List<TestResult>();
        }


        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private List<TestResult> ParseTestResults()
        {
            var testResults = new List<TestResult>();
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(_xmlResultFile);

                XmlNodeList testsuiteNodes = xmlDocument.DocumentElement.SelectNodes("/testsuites/testsuite");
                foreach (XmlNode testsuiteNode in testsuiteNodes)
                {
                    XmlNodeList testcaseNodes = testsuiteNode.SelectNodes("testcase");
                    testResults.AddRange(testcaseNodes.AsParallel().Cast<XmlNode>().Select(TryParseTestResult).Where(tr => tr != null));
                }
            }
            catch (XmlException e)
            {
                _testEnvironment.DebugWarning(
                    $"Test result file {_xmlResultFile} could not be parsed (completely) - your test executable has probably crashed. Exception message: {e.Message}");
            }

            return testResults;
        }

        private TestResult TryParseTestResult(XmlNode testcaseNode)
        {
            try
            {
                return ParseTestResult(testcaseNode);
            }
            catch (Exception e)
            {
                _testEnvironment.DebugWarning(
                    $"XmlNode could not be parsed: \'{GetQualifiedName(testcaseNode)}\'. Exception message: {e.Message}");
                return null;
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private TestResult ParseTestResult(XmlNode testcaseNode)
        {
            string qualifiedName = GetQualifiedName(testcaseNode);

            TestCase testCase;
            if (!_testCasesMap.TryGetValue(qualifiedName, out testCase))
                return null;

            var testResult = new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName
            };

            string duration = testcaseNode.Attributes["time"].InnerText;
            testResult.Duration = ParseDuration(duration);

            string testCaseStatus = testcaseNode.Attributes["status"].InnerText;
            switch (testCaseStatus)
            {
                case "run":
                    XmlNodeList failureNodes = testcaseNode.SelectNodes("failure");
                    if (failureNodes.Count == 0)
                    {
                        testResult.Outcome = TestOutcome.Passed;
                    }
                    else
                    {
                        var parser = new ErrorMessageParser(failureNodes, _baseDir);
                        parser.Parse();
                        testResult.Outcome = TestOutcome.Failed;
                        testResult.ErrorMessage = parser.ErrorMessage;
                        testResult.ErrorStackTrace = parser.ErrorStackTrace;
                    }
                    break;
                case "notrun":
                    testResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    string msg = "Unknown testcase status: " + testCaseStatus;
                    _testEnvironment.LogError(msg);
                    throw new Exception(msg);
            }

            return testResult;
        }

        private string GetQualifiedName(XmlNode testcaseNode)
        {
            if (testcaseNode == null)
                return "<XmlNode is null>";
            if (testcaseNode.Attributes == null)
                return "<XmlNode has no attributes>";

            string className = testcaseNode.Attributes["classname"]?.InnerText ?? "<unknown classname>";
            string testCaseName = testcaseNode.Attributes["name"]?.InnerText ?? "<unknown testcasename>";
            return $"{className}.{testCaseName}";
        }

        private TimeSpan ParseDuration(string durationString)
        {
            double duration = double.Parse(durationString, NumberFormatInfo);
            if (duration <= 0.001)
            {
                duration = 0.001;
            }
            return TimeSpan.FromSeconds(duration);
        }

    }

}