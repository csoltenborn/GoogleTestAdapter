// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class XmlTestResultParser
    {
        private static readonly NumberFormatInfo NumberFormatInfo = new CultureInfo("en-US").NumberFormat;


        private readonly ILogger _logger;
        private readonly string _xmlResultFile;
        private readonly IDictionary<string, TestCase> _testCasesMap;


        public XmlTestResultParser(IEnumerable<TestCase> testCasesRun, string xmlResultFile, ILogger logger)
        {
            _logger = logger;
            _xmlResultFile = xmlResultFile;
            _testCasesMap = testCasesRun.ToDictionary(tc => tc.FullyQualifiedName, tc => tc);
        }


        public List<TestResult> GetTestResults()
        {
            if (File.Exists(_xmlResultFile))
            {
                return ParseTestResults();
            }

            _logger.LogWarning(Resources.OutputFileMissing);
            return new List<TestResult>();
        }


        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private List<TestResult> ParseTestResults()
        {
            var testResults = new List<TestResult>();
            try
            {
                var settings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
                settings.XmlResolver = null;
                using (var reader = XmlReader.Create(_xmlResultFile, settings))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(reader);

                    XmlNodeList testsuiteNodes = xmlDocument.DocumentElement.SelectNodes("/testsuites/testsuite");
                    foreach (XmlNode testsuiteNode in testsuiteNodes)
                    {
                        XmlNodeList testcaseNodes = testsuiteNode.SelectNodes("testcase");
                        testResults.AddRange(testcaseNodes
                                                .AsParallel()
                                                .Cast<XmlNode>()
                                                .Select(TryParseTestResult)
                                                .Where(tr => tr != null));
                    }
                }
            }
            catch (XmlException e)
            {
                _logger.DebugWarning(String.Format(Resources.TestResultParse, _xmlResultFile, e.Message));
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
                _logger.DebugWarning(String.Format(Resources.XmlNodeParse, GetQualifiedName(testcaseNode), e.Message));
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
                        var parser = new ErrorMessageParser(failureNodes);
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
                    string msg = String.Format(Resources.UnknownTestCase, testCaseStatus);
                    _logger.LogError(msg);
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

        private TimeSpan ParseDuration(string durationInSeconds)
        {
            return
                StandardOutputTestResultParser
                    .NormalizeDuration(TimeSpan.FromSeconds(double.Parse(durationInSeconds, NumberFormatInfo)));
        }

    }

}