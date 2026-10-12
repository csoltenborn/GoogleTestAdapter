using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public const string GtaExitCodeOutputBegin = "GTA_EXIT_CODE_OUTPUT_BEGIN";
        public const string GtaExitCodeOutputEnd = "GTA_EXIT_CODE_OUTPUT_END";
        public const string GtaExitCodeSkip = "GTA_EXIT_CODE_SKIP";

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
        public TestCase TimedOutTestCase { get; private set; }
        public IList<TestResult> TestResults { get; } = new List<TestResult>();
        public IList<string> ExitCodeOutput { get; } = new List<string>();
        public bool ExitCodeSkip { get; private set; } = false;

        private readonly List<TestCase> _testCasesRun;
        private readonly ILogger _logger;
        private readonly ITestFrameworkReporter _reporter;

        // output lines, the end of the output, and timeouts are reported by different threads
        private readonly object _lock = new object();
        private readonly List<string> _consoleOutput = new List<string>();
        private bool _isParsingExitCodeOutput;

        private class RunningTest
        {
            public TestCase TestCase { get; set; }
            public Stopwatch Stopwatch { get; set; }
        }
        private volatile RunningTest _runningTest;
        private TimeSpan _timeout;
        private int? _exitCode;

        static StreamingStandardOutputTestResultParser()
        {
            string passedMarker = Regex.Escape(Passed);
            string failedMarker = Regex.Escape(Failed);
            PrefixedLineRegex = new Regex($"(.+)((?:{passedMarker}|{failedMarker}).*)", RegexOptions.Compiled);
        }

        public StreamingStandardOutputTestResultParser(IEnumerable<TestCase> testCasesRun,
                ILogger logger, ITestFrameworkReporter reporter, SourcePathMapper sourcePathMapper = null)
        {
            _testCasesRun = testCasesRun.ToList();
            _logger = logger;
            _reporter = reporter;
            _sourcePathMapper = sourcePathMapper ?? SourcePathMapper.Identity;
        }

        private readonly SourcePathMapper _sourcePathMapper;

        /// <summary>
        /// Thread safe.
        /// </summary>
        public void ReportLine(string line)
        {
            lock (_lock)
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
        }

        private void DoReportLine(string line)
        {
            if (IsPassedLine(line) || IsFailedLine(line) || IsSkippedLine(line))
                _runningTest = null;

            if (IsRunLine(line) || line.StartsWith(GtaExitCodeOutputBegin))
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
                    _isParsingExitCodeOutput = true;
                    return;
                }
            }

            if (line.StartsWith(GtaExitCodeOutputEnd))
            {
                _consoleOutput.ForEach(l => ExitCodeOutput.Add(l));
                _consoleOutput.Clear();
                _isParsingExitCodeOutput = false;
                return;
            }

            if (line.StartsWith(GtaExitCodeSkip))
            {
                ExitCodeSkip = true;
                return;
            }

            _consoleOutput.Add(line);
        }

        /// <param name="exitCode">the exit code of the test executable, which is added to the error message of a crashed test</param>
        public void Flush(int? exitCode = null)
        {
            lock (_lock)
            {
                _exitCode = exitCode;
                if (_consoleOutput.Count > 0)
                {
                    if (_isParsingExitCodeOutput)
                    {
                        _consoleOutput.ForEach(l => ExitCodeOutput.Add(l));
                        _isParsingExitCodeOutput = false;
                    }
                    else
                    {
                        ReportTestResult();
                    }

                    _consoleOutput.Clear();
                }
            }
        }

        private void ReportTestStart(string line)
        {
            string qualifiedTestname = RemovePrefix(line).Trim();
            TestCase testCase = FindTestcase(qualifiedTestname, _testCasesRun);
            _runningTest = testCase == null ? null : new RunningTest { TestCase = testCase, Stopwatch = Stopwatch.StartNew() };
            if (testCase != null)
                _reporter.ReportTestsStarted(testCase.Yield());
        }

        /// <summary>
        /// The test which is currently running, and for how long it has been running. Thread safe.
        /// </summary>
        public TestCase GetRunningTestCase(out TimeSpan runningFor)
        {
            RunningTest runningTest = _runningTest;
            runningFor = runningTest?.Stopwatch.Elapsed ?? TimeSpan.Zero;
            return runningTest?.TestCase;
        }

        /// <summary>
        /// Marks a test as timed out, i.e., once the test executable has been killed, the test's result will be
        /// a failure due to the timeout rather than due to a crash. Thread safe.
        /// </summary>
        public void SetTimedOut(TestCase testCase, TimeSpan timeout)
        {
            lock (_lock)
            {
                _timeout = timeout;
                TimedOutTestCase = testCase;
            }
        }

        public static string CreateTimeoutText(TimeSpan timeout)
        {
            return $"!! This test has TIMED OUT after {timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s (CMake test property TIMEOUT) !!";
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
                return CreateCrashedOrTimedOutTestResult(testCase, "");
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
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg, testCase.FullyQualifiedName, _sourcePathMapper);
                parser.Parse();
                return WithStandardOutput(CreateFailedTestResult(
                    testCase,
                    ParseDuration(line, _logger),
                    parser.ErrorMessage,
                    parser.ErrorStackTrace), errorMsg);
            }
            if (IsPassedLine(line))
            {
                return WithStandardOutput(CreatePassedTestResult(testCase, ParseDuration(line, _logger)), errorMsg);
            }
            if (IsSkippedLine(line))
            {
                // just like for failed tests, the skip message is the error message
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg, testCase.FullyQualifiedName, _sourcePathMapper);
                parser.Parse();
                return WithStandardOutput(CreateSkippedTestResult(
                    testCase,
                    ParseDuration(line, _logger),
                    parser.ErrorMessage,
                    parser.ErrorStackTrace), errorMsg);
            }

            return CreateCrashedOrTimedOutTestResult(testCase, errorMsg);
        }

        // the complete output of the test as printed by Google Test, including failure messages (the console output
        // does not allow to tell where a failure message ends and further output of the test begins)
        private static TestResult WithStandardOutput(TestResult testResult, string testOutput)
        {
            if (testOutput != "")
                testResult.StandardOutput = testOutput;
            return testResult;
        }

        private TestResult CreateCrashedOrTimedOutTestResult(TestCase testCase, string testOutput)
        {
            bool isTimedOut = testCase == TimedOutTestCase;
            if (!isTimedOut)
                CrashedTestCase = testCase;

            string message = isTimedOut ? CreateTimeoutText(_timeout) : CrashText + CreateExitCodeText(_exitCode);
            return WithStandardOutput(CreateFailedTestResult(
                testCase,
                isTimedOut ? _timeout : TimeSpan.FromMilliseconds(0),
                message,
                CreateTestLocationStackTrace(testCase)), testOutput);
        }

        // a crashed or timed out test has no failure location, so point to the test itself
        private static string CreateTestLocationStackTrace(TestCase testCase)
        {
            return string.IsNullOrEmpty(testCase.CodeFilePath)
                ? ""
                : ErrorMessageParser.CreateStackTraceEntry(
                    testCase.FullyQualifiedName, testCase.CodeFilePath, testCase.LineNumber.ToString());
        }

        // exit codes of crashes are usually NTSTATUS codes, e.g. 0xC0000005 for an access violation
        public static string CreateExitCodeText(int? exitCode)
        {
            return exitCode.HasValue ? $" (exit code {exitCode.Value}, i.e. 0x{exitCode.Value:X8})" : "";
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

        private TestResult CreateSkippedTestResult(TestCase testCase, TimeSpan duration, string errorMessage, string errorStackTrace)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Skipped,
                ErrorMessage = errorMessage == "" ? null : errorMessage,
                ErrorStackTrace = errorStackTrace == "" ? null : errorStackTrace,
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