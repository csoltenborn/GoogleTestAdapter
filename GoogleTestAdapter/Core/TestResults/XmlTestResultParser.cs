// This file has been modified by Microsoft on 6/2017.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.TestResults
{
    public class XmlTestResultParser
    {
        private const string ErrorMsgNoXmlFile = "Output file does not exist, did your tests crash?";

        private static readonly NumberFormatInfo NumberFormatInfo = new CultureInfo("en-US").NumberFormat;

        private static readonly Regex SpecialCharsRegex = new Regex("[äöüÄÖÜß]+", RegexOptions.Compiled);


        private readonly ILogger _logger;
        private readonly string _xmlResultFile;
        private readonly string _testExecutable;
        private readonly IDictionary<string, TestCase> _testCasesMap;
        private readonly Lazy<IDictionary<string, TestCase>> _workaroundMapLazy;


        public XmlTestResultParser(IEnumerable<TestCase> testCasesRun, string testExecutable, string xmlResultFile, ILogger logger)
        {
            _logger = logger;
            _testExecutable = testExecutable;
            _xmlResultFile = xmlResultFile;
            _testCasesMap = testCasesRun.ToDictionary(tc => tc.FullyQualifiedName, tc => tc);
            _workaroundMapLazy  = new Lazy<IDictionary<string, TestCase>>(CreateWorkaroundMap);
        }

        // workaround for special chars not ending up in gtest xml output file
        private IDictionary<string, TestCase> CreateWorkaroundMap()
        {
            var map = new Dictionary<string, TestCase>();
            var duplicates = new HashSet<string>();
            var duplicateTestCases = new List<TestCase>();

            foreach (var kvp in _testCasesMap)
            {
                string workaroundKey = SpecialCharsRegex.Replace(kvp.Key, "");
                if (map.ContainsKey(workaroundKey))
                {
                    duplicates.Add(workaroundKey);
                    duplicateTestCases.Add(kvp.Value);
                }
                else
                {
                    map[workaroundKey] = kvp.Value;
                }
            }

            foreach (var duplicate in duplicates)
            {
                duplicateTestCases.Add(map[duplicate]);
                map.Remove(duplicate);
            }

            if (duplicateTestCases.Any())
            {
                string message =
                    $"Executable {_testExecutable} has test names containing characters which happen to not end up in the XML file produced by Google Test. " +
                    "This has caused ambiguities while resolving the according test results, which are thus not available. " + 
                    $"Note that this problem does not occur if GTA option '{SettingsWrapper.OptionUseNewTestExecutionFramework}' is enabled." +
                    $"\nThe following tests are affected: {string.Join(", ", duplicateTestCases.Select(tc => tc.FullyQualifiedName).OrderBy(n => n))}";
                _logger.LogWarning(message);
            }

            return map;
        }


        public List<TestResult> GetTestResults()
        {
            if (File.Exists(_xmlResultFile))
            {
                return ParseTestResults();
            }

            _logger.LogWarning(ErrorMsgNoXmlFile);
            return new List<TestResult>();
        }


        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private List<TestResult> ParseTestResults()
        {
            var testResults = new List<TestResult>();
            try
            {
#pragma warning disable IDE0017 // Simplify object initialization
                var settings = new XmlReaderSettings(); // Don't use an object initializer for FxCop to understand.
#pragma warning restore IDE0017 // Simplify object initialization
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
                _logger.DebugWarning(
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
                _logger.DebugWarning(
                    $"XmlNode could not be parsed: \'{GetQualifiedName(testcaseNode)}\'. Exception message: {e.Message}");
                return null;
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private TestResult ParseTestResult(XmlNode testcaseNode)
        {
            string qualifiedName = GetQualifiedName(testcaseNode);

            if (!_testCasesMap.TryGetValue(qualifiedName, out var testCase) &&
                !_workaroundMapLazy.Value.TryGetValue(qualifiedName, out testCase))
            {
                return null;
            }

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
                case "skipped":
                case "notrun":
                    testResult.Outcome = TestOutcome.Skipped;
                    break;
                default:
                    string msg = "Unknown testcase status: " + testCaseStatus;
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
                StreamingStandardOutputTestResultParser
                    .NormalizeDuration(TimeSpan.FromSeconds(double.Parse(durationInSeconds, NumberFormatInfo)));
        }

    }

}