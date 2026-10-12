// This file has been modified by Microsoft on 6/2017.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
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


        private readonly SourcePathMapper _sourcePathMapper;

        public XmlTestResultParser(IEnumerable<TestCase> testCasesRun, string testExecutable, string xmlResultFile, ILogger logger,
            SourcePathMapper sourcePathMapper = null)
        {
            _logger = logger;
            _sourcePathMapper = sourcePathMapper ?? SourcePathMapper.Identity;
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
                    $"Note that this problem can only occur if GTA option '{SettingsWrapper.OptionDebuggerKind}' is '{DebuggerKind.VsTestFramework.ToReadableString()}'." +
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
                using (var reader = XmlReader.Create(new StringReader(GoogleTestXmlFile.ReadAllText(_xmlResultFile, _logger)), settings))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(reader);

                    XmlNodeList testsuiteNodes = xmlDocument.DocumentElement.SelectNodes("/testsuites/testsuite");
                    foreach (XmlNode testsuiteNode in testsuiteNodes)
                    {
                        XmlNodeList testcaseNodes = testsuiteNode.SelectNodes("testcase");
                        List<TestResult> suiteResults = testcaseNodes
                                                .AsParallel()
                                                .Cast<XmlNode>()
                                                .Select(TryParseTestResult)
                                                .Where(tr => tr != null)
                                                .ToList();
                        testResults.AddRange(WithTestSuiteFixtureResults(testsuiteNode, testcaseNodes, suiteResults));
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

        // Google Test reports failures of SetUpTestSuite() and TearDownTestSuite() as test cases without name. The tests
        // of a suite whose SetUpTestSuite() has failed are skipped without having been started (timestamp 1970), the
        // first nameless failure is then the one of SetUpTestSuite(); other nameless failures are the ones of
        // TearDownTestSuite().
        private List<TestResult> WithTestSuiteFixtureResults(XmlNode testsuiteNode, XmlNodeList testcaseNodes, List<TestResult> suiteResults)
        {
            string suite = testsuiteNode.Attributes?["name"]?.InnerText;
            if (string.IsNullOrEmpty(suite))
                return suiteResults;

            List<XmlNode> fixtureFailures = testcaseNodes.Cast<XmlNode>()
                .Where(node => node.Attributes?["name"]?.InnerText == "" && node.SelectNodes("failure")?.Count > 0)
                .ToList();
            List<XmlNode> tests = testcaseNodes.Cast<XmlNode>()
                .Where(node => !string.IsNullOrEmpty(node.Attributes?["name"]?.InnerText))
                .ToList();

            bool isSetUpTestSuiteFailed = fixtureFailures.Any() && tests.Any() && tests.All(node =>
                node.Attributes["result"]?.InnerText == "skipped" &&
                (node.Attributes["timestamp"]?.InnerText ?? "").StartsWith("1970-", StringComparison.Ordinal));
            if (isSetUpTestSuiteFailed)
            {
                var parser = new ErrorMessageParser(fixtureFailures[0].SelectNodes("failure"), TestSuiteFixtureResults.GetSetUpTestSuiteName(suite), _sourcePathMapper);
                parser.Parse();
                suiteResults = suiteResults
                    .Select(result => result.Outcome == TestOutcome.Skipped
                        ? TestSuiteFixtureResults.CreateSetUpTestSuiteFailedResult(result, parser.ErrorMessage, parser.ErrorStackTrace)
                        : result)
                    .ToList();
                fixtureFailures.RemoveAt(0);
            }

            TestCase tearDownTestCase = TestSuiteFixtureResults.FindTearDownTestSuiteTestCase(suite, _testCasesMap.Values);
            if (tearDownTestCase != null)
            {
                string errorMessage = null, errorStackTrace = null;
                if (fixtureFailures.Any())
                {
                    var parser = new ErrorMessageParser(fixtureFailures[0].SelectNodes("failure"), tearDownTestCase.FullyQualifiedName, _sourcePathMapper);
                    parser.Parse();
                    errorMessage = parser.ErrorMessage;
                    errorStackTrace = parser.ErrorStackTrace;
                }
                suiteResults.Add(TestSuiteFixtureResults.CreateTearDownTestSuiteResult(tearDownTestCase, errorMessage, errorStackTrace, null));
            }

            return suiteResults;
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
                    // tests skipped with GTEST_SKIP() have status "run" (Google Test >= 1.10)
                    if (testcaseNode.Attributes["result"]?.InnerText == "skipped")
                    {
                        var skipParser = new ErrorMessageParser(testcaseNode.SelectNodes("skipped"), testCase.FullyQualifiedName, _sourcePathMapper);
                        skipParser.Parse();
                        testResult.Outcome = TestOutcome.Skipped;
                        testResult.ErrorMessage = skipParser.ErrorMessage == "" ? null : skipParser.ErrorMessage;
                        testResult.ErrorStackTrace = skipParser.ErrorStackTrace == "" ? null : skipParser.ErrorStackTrace;
                        break;
                    }

                    XmlNodeList failureNodes = testcaseNode.SelectNodes("failure");
                    if (failureNodes.Count == 0)
                    {
                        testResult.Outcome = TestOutcome.Passed;
                    }
                    else
                    {
                        var parser = new ErrorMessageParser(failureNodes, testCase.FullyQualifiedName, _sourcePathMapper);
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