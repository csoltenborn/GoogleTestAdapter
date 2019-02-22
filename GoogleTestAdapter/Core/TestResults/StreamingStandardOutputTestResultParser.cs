using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string Run = "[ RUN      ]";
        private const string Failed = "[  FAILED  ]";
        private const string Passed = "[       OK ]";
        private const string Skipped = "[  SKIPPED ]";

        public const string GtaResultCodeOutputBegin = "GTA_RESULT_CODE_OUTPUT_BEGIN";
        public const string GtaResultCodeOutputEnd = "GTA_RESULT_CODE_OUTPUT_END";

        public const string CrashText = "!! This test has probably CRASHED !!";

        /// <summary>
        /// Google Test reports test duration in complete ms. In case of 0ms,
        /// we assume the actual duration to be &lt;0.5ms, and thus go for 0.25ms on average
        /// (which also makes VS display the duration properly as "&lt;1ms").
        /// 2500 ticks = 0.25ms
        /// </summary>
        public static readonly TimeSpan ShortTestDuration = TimeSpan.FromTicks(2500);

        private static readonly Regex PrefixedLineRegex;

        public TestCase CrashedTestCase { get; private set; }
        public IList<TestResult> TestResults { get; } = new List<TestResult>();
        public IList<string> ResultCodeOutput { get; } = new List<string>();

        private readonly List<TestCase> _testCasesRun;
        private readonly ILogger _logger;
        private readonly ITestFrameworkReporter _reporter;

        private readonly List<string> _consoleOutput = new List<string>();
        private bool _isParsingResultCodeOutput;

        static StreamingStandardOutputTestResultParser()
        {
            string passedMarker = Regex.Escape(Passed);
            string failedMarker = Regex.Escape(Failed);
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
            if (IsRunLine(line) || line.StartsWith(GtaResultCodeOutputBegin))
            {
                if (_consoleOutput.Count > 0)
                {
                    ReportTestResult();
                    _consoleOutput.Clear();
                }

                if (IsRunLine(line))
                {
                    ReportTestStart(line);
                }
                else
                {
                    _isParsingResultCodeOutput = true;
                    return;
                }
            }

            if (line.StartsWith(GtaResultCodeOutputEnd))
            {
                _consoleOutput.ForEach(l => ResultCodeOutput.Add(l));
                _consoleOutput.Clear();
                _isParsingResultCodeOutput = false;
                return;
            }

            _consoleOutput.Add(line);
        }

        public void Flush()
        {
            if (_consoleOutput.Count > 0)
            {
                if (_isParsingResultCodeOutput)
                {
                    _consoleOutput.ForEach(l => ResultCodeOutput.Add(l));
                    _isParsingResultCodeOutput = false;
                }
                else
                {
                    ReportTestResult();
                }

                _consoleOutput.Clear();
            }
        }

        private void ReportTestStart(string line)
        {
            string qualifiedTestname = RemovePrefix(line).Trim();
            TestCase testCase = FindTestcase(qualifiedTestname, _testCasesRun);
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
                !IsRunLine(_consoleOutput[currentLineIndex]))
                currentLineIndex++;

            if (currentLineIndex == _consoleOutput.Count)
                return null;

            string line = _consoleOutput[currentLineIndex++];
            string qualifiedTestname = RemovePrefix(line).Trim();
            TestCase testCase = FindTestcase(qualifiedTestname, _testCasesRun);
            if (testCase == null)
            {
                _logger.DebugWarning($"No known test case for test result of line '{line}'' - are you repeating a test run, but tests have changed in the meantime?");
                return null;
            }

            if (currentLineIndex == _consoleOutput.Count)
            {
                CrashedTestCase = testCase;
                return CreateFailedTestResult(
                    testCase,
                    TimeSpan.FromMilliseconds(0),
                    CrashText,
                    "");
            }

            line = _consoleOutput[currentLineIndex++];

            string errorMsg = "";
            while (!(IsFailedLine(line) || IsPassedLine(line) || IsSkippedLine(line))
                && currentLineIndex <= _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = currentLineIndex < _consoleOutput.Count ? _consoleOutput[currentLineIndex] : "";
                currentLineIndex++;
            }
            if (IsFailedLine(line))
            {
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg);
                parser.Parse();
                return CreateFailedTestResult(
                    testCase,
                    ParseDuration(line, _logger),
                    parser.ErrorMessage,
                    parser.ErrorStackTrace);
            }
            if (IsPassedLine(line))
            {
                return CreatePassedTestResult(testCase, ParseDuration(line, _logger));
            }
            if (IsSkippedLine(line))
            {
                return CreateSkippedTestResult(testCase, ParseDuration(line, _logger));
            }

            CrashedTestCase = testCase;
            string message = CrashText;
            message += errorMsg == "" ? "" : $"\nTest output:\n\n{errorMsg}";
            TestResult result = CreateFailedTestResult(
                testCase,
                TimeSpan.FromMilliseconds(0),
                message,
                "");
            return result;
        }

        private TimeSpan ParseDuration(string line, ILogger logger)
        {
            int durationInMs = 1;
            try
            {
                // duration is a 64-bit number (no decimals) in the user's locale
                int indexOfOpeningBracket = line.LastIndexOf('(');
                int lengthOfDurationPart = line.Length - indexOfOpeningBracket - 2;
                string durationPart = line.Substring(indexOfOpeningBracket + 1, lengthOfDurationPart);
                durationPart = durationPart.Replace("ms", "").Trim();
                durationInMs = Int32.Parse(durationPart, NumberStyles.Number);
            }
            catch (Exception)
            {
                logger.LogWarning("Could not parse duration in line '" + line + "'");
            }

            return NormalizeDuration(TimeSpan.FromMilliseconds(durationInMs));
        }

        public static TimeSpan NormalizeDuration(TimeSpan duration)
        {
            return duration.TotalMilliseconds < 1
                ? ShortTestDuration
                : duration;
        }

        public static TestResult CreatePassedTestResult(TestCase testCase, TimeSpan duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Passed,
                Duration = duration
            };
        }

        private TestResult CreateSkippedTestResult(TestCase testCase, TimeSpan duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Skipped,
                Duration = duration
            };
        }

        public static TestResult CreateFailedTestResult(TestCase testCase, TimeSpan duration, string errorMessage, string errorStackTrace)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Failed,
                ErrorMessage = errorMessage,
                ErrorStackTrace = errorStackTrace,
                Duration = duration
            };
        }

        private TestCase FindTestcase(string qualifiedTestname, IList<TestCase> testCasesRun)
        {
            return testCasesRun.SingleOrDefault(tc => tc.FullyQualifiedName == qualifiedTestname);
        }

        private bool IsRunLine(string line)
        {
            return line.StartsWith(Run);
        }

        private bool IsPassedLine(string line)
        {
            return line.StartsWith(Passed);
        }

        private bool IsFailedLine(string line)
        {
            return line.StartsWith(Failed);
        }

        private bool IsSkippedLine(string line)
        {
            return line.StartsWith(Skipped);
        }

        private string RemovePrefix(string line)
        {
            return line.Substring(Run.Length);
        }
    }

}