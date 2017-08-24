// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class StreamingStandardOutputTestResultParser
    {
        public static readonly Regex PrefixedLineRegex;

        public TestCase CrashedTestCase { get; private set; }
        public IList<TestResult> TestResults { get; } = new List<TestResult>();

        private readonly List<TestCase> _testCasesRun;
        private readonly ILogger _logger;
        private readonly ITestFrameworkReporter _reporter;

        private readonly List<string> _consoleOutput = new List<string>();

        static StreamingStandardOutputTestResultParser()
        {
            string passedMarker = Regex.Escape(StandardOutputTestResultParser.Passed);
            string failedMarker = Regex.Escape(StandardOutputTestResultParser.Failed);
            PrefixedLineRegex = new Regex($"(.+)((?:{passedMarker}|{failedMarker}).*)", RegexOptions.Compiled);
        }

        public StreamingStandardOutputTestResultParser(IEnumerable<TestCase> testCasesRun,
                ILogger logger, ITestFrameworkReporter reporter)
        {
            _testCasesRun = testCasesRun.ToList();
            _logger = logger;
            _reporter = reporter;
        }

        public void ReportLine(string line)
        {
            Match testEndMatch = PrefixedLineRegex.Match(line);
            if (testEndMatch.Success)
            {
                string restOfErrorMessage = testEndMatch.Groups[1].Value;
                if (!string.IsNullOrEmpty(restOfErrorMessage))
                    DoReportLine(restOfErrorMessage);

                string testEndPart = testEndMatch.Groups[2].Value;
                DoReportLine(testEndPart);
            }
            else
            {
                DoReportLine(line);
            }
        }

        private void DoReportLine(string line)
        {
            if (StandardOutputTestResultParser.IsRunLine(line))
            {
                if (_consoleOutput.Count > 0)
                {
                    ReportTestResult();
                    _consoleOutput.Clear();
                }
                ReportTestStart(line);
            }
            _consoleOutput.Add(line);
        }

        public void Flush()
        {
            if (_consoleOutput.Count > 0)
            {
                ReportTestResult();
                _consoleOutput.Clear();
            }
        }

        private void ReportTestStart(string line)
        {
            string qualifiedTestname = StandardOutputTestResultParser.RemovePrefix(line).Trim();
            TestCase testCase = StandardOutputTestResultParser.FindTestcase(qualifiedTestname, _testCasesRun);
            if (testCase != null)
                _reporter.ReportTestsStarted(testCase.Yield());
        }

        private void ReportTestResult()
        {
            TestResult result = CreateTestResult();
            if (result != null)
            {
                _reporter.ReportTestResults(result.Yield());
                TestResults.Add(result);
            }
        }

        private TestResult CreateTestResult()
        {
            int currentLineIndex = 0;
            while (currentLineIndex < _consoleOutput.Count &&
                !StandardOutputTestResultParser.IsRunLine(_consoleOutput[currentLineIndex]))
                currentLineIndex++;

            if (currentLineIndex == _consoleOutput.Count)
                return null;

            string line = _consoleOutput[currentLineIndex++];
            string qualifiedTestname = StandardOutputTestResultParser.RemovePrefix(line).Trim();
            TestCase testCase = StandardOutputTestResultParser.FindTestcase(qualifiedTestname, _testCasesRun);
            if (testCase == null)
            {
                _logger.DebugWarning(String.Format(Resources.NoKnownTestCaseMessage, line));
                return null;
            }

            if (currentLineIndex == _consoleOutput.Count)
            {
                CrashedTestCase = testCase;
                return StandardOutputTestResultParser.CreateFailedTestResult(
                    testCase,
                    TimeSpan.FromMilliseconds(0),
                    StandardOutputTestResultParser.CrashText,
                    "");
            }

            line = _consoleOutput[currentLineIndex++];

            string errorMsg = "";
            while (
                !(StandardOutputTestResultParser.IsFailedLine(line)
                    || StandardOutputTestResultParser.IsPassedLine(line))
                && currentLineIndex <= _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = currentLineIndex < _consoleOutput.Count ? _consoleOutput[currentLineIndex] : "";
                currentLineIndex++;
            }
            if (StandardOutputTestResultParser.IsFailedLine(line))
            {
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg);
                parser.Parse();
                return StandardOutputTestResultParser.CreateFailedTestResult(
                    testCase,
                    StandardOutputTestResultParser.ParseDuration(line, _logger),
                    parser.ErrorMessage,
                    parser.ErrorStackTrace);
            }
            if (StandardOutputTestResultParser.IsPassedLine(line))
            {
                return StandardOutputTestResultParser.CreatePassedTestResult(
                    testCase,
                    StandardOutputTestResultParser.ParseDuration(line, _logger));
            }

            CrashedTestCase = testCase;
            string message = StandardOutputTestResultParser.CrashText;
            message += errorMsg == "" ? "" : $"\nTest output:\n\n{errorMsg}";
            TestResult result = StandardOutputTestResultParser.CreateFailedTestResult(
                testCase,
                TimeSpan.FromMilliseconds(0),
                message,
                "");
            return result;
        }

    }

}