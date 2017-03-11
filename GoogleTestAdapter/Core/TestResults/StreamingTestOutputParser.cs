using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class StreamingTestOutputParser
    {
        public const string CrashText = "!! This test has probably CRASHED !!";

        /// <summary>
        /// 1000 ticks = 0.1ms to make sure VS shows "&lt;1ms"
        /// </summary>
        public static readonly TimeSpan ShortTestDuration = TimeSpan.FromTicks(1000);

        private const string Run = "[ RUN      ]";
        private const string Failed = "[  FAILED  ]";
        private const string Passed = "[       OK ]";

        private static readonly Regex PrefixedLineRegex;

        public TestCase CrashedTestCase { get; private set; }
        public IList<TestResult> TestResults { get; } = new List<TestResult>();

        private readonly List<TestCase> _testCasesRun;
        private readonly ILogger _logger;
        private readonly string _baseDir;
        private readonly ITestFrameworkReporter _reporter;

        private readonly List<string> _consoleOutput = new List<string>();

        static StreamingTestOutputParser()
        {
            string passedMarker = Regex.Escape(Passed);
            string failedMarker = Regex.Escape(Failed);
            PrefixedLineRegex = new Regex($"(.*)((?:{passedMarker}|{failedMarker}).*)", RegexOptions.Compiled);
        }

        public StreamingTestOutputParser(IEnumerable<TestCase> testCasesRun,
                ILogger logger, string baseDir, ITestFrameworkReporter reporter)
        {
            _testCasesRun = testCasesRun.ToList();
            _logger = logger;
            _baseDir = baseDir;
            _reporter = reporter;
        }

        public void ReportLine(string line)
        {
            Match testEndMatch = PrefixedLineRegex.Match(line);
            if (testEndMatch.Success)
            {
                string restOfErrorMessage = testEndMatch.Groups[1].Value;
                if (!String.IsNullOrEmpty(restOfErrorMessage))
                    DoReportLine(restOfErrorMessage);

                string testEndPart = testEndMatch.Groups[2].Value;
                DoReportLine(testEndPart);
            }
            else
            {
                DoReportLine(line);
            }
        }

        public void Flush()
        {
            if (_consoleOutput.Count > 0)
            {
                ReportTestResult();
                _consoleOutput.Clear();
            }
        }

        internal static TimeSpan NormalizeDuration(TimeSpan duration)
        {
            return duration.TotalMilliseconds < 1
                ? ShortTestDuration
                : duration;
        }

        private void DoReportLine(string line)
        {
            if (IsRunLine(line))
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

        private void ReportTestStart(string line)
        {
            string qualifiedTestname = RemovePrefix(line).Trim();
            TestCase testCase = FindTestcase(qualifiedTestname, _testCasesRun);
            _reporter.ReportTestStarted(testCase);
        }

        private void ReportTestResult()
        {
            TestResult result = CreateTestResult();
            if (result != null)
            {
                _reporter.ReportTestResult(result);
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
            while (
                !(IsFailedLine(line)
                    || IsPassedLine(line))
                && currentLineIndex <= _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = currentLineIndex < _consoleOutput.Count ? _consoleOutput[currentLineIndex] : "";
                currentLineIndex++;
            }
            if (IsFailedLine(line))
            {
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg, _baseDir);
                parser.Parse();
                return CreateFailedTestResult(
                    testCase,
                    ParseDuration(line),
                    parser.ErrorMessage,
                    parser.ErrorStackTrace);
            }
            if (IsPassedLine(line))
            {
                return CreatePassedTestResult(
                    testCase,
                    ParseDuration(line));
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

        private TimeSpan ParseDuration(string line)
        {
            int durationInMs = 1;
            try
            {
                // TODO check format in gtest code, replace with regex
                int indexOfOpeningBracket = line.LastIndexOf('(');
                int lengthOfDurationPart = line.Length - indexOfOpeningBracket - 2;
                string durationPart = line.Substring(indexOfOpeningBracket + 1, lengthOfDurationPart);
                durationPart = durationPart.Replace("ms", "").Trim();
                durationInMs = Int32.Parse(durationPart);
            }
            catch (Exception)
            {
                _logger.LogWarning("Could not parse duration in line '" + line + "'");
            }

            return NormalizeDuration(TimeSpan.FromMilliseconds(durationInMs));
        }

        private TestResult CreatePassedTestResult(TestCase testCase, TimeSpan duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Passed,
                Duration = duration
            };
        }

        private TestResult CreateFailedTestResult(TestCase testCase, TimeSpan duration, string errorMessage, string errorStackTrace)
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
            return testCasesRun.Single(tc => tc.FullyQualifiedName == qualifiedTestname);
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

        private string RemovePrefix(string line)
        {
            return line.Substring(Run.Length);
        }
    }

}