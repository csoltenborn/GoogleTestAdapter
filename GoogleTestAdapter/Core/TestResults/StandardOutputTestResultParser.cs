// This file has been modified by Microsoft on 9/2017.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class StandardOutputTestResultParser
    {
        private const string Run = "[ RUN      ]";
        public const string Failed = "[  FAILED  ]";
        public const string Passed = "[       OK ]";

        public static readonly string CrashText = Resources.CrashText;

        /// <summary>
        /// 1000 ticks = 0.1ms to make sure VS shows "&lt;1ms"
        /// </summary>
        public static readonly TimeSpan ShortTestDuration = TimeSpan.FromTicks(1000);

        public TestCase CrashedTestCase { get; private set; }

        private readonly List<string> _consoleOutput;
        private readonly List<TestCase> _testCasesRun;
        private readonly ILogger _logger;


        public StandardOutputTestResultParser(IEnumerable<TestCase> testCasesRun, IEnumerable<string> consoleOutput, ILogger logger)
        {
            _consoleOutput = consoleOutput.ToList();
            _testCasesRun = testCasesRun.ToList();
            _logger = logger;
        }


        public List<TestResult> GetTestResults()
        {
            var testResults = new List<TestResult>();
            int indexOfNextTestcase = FindIndexOfNextTestcase(0);
            while (indexOfNextTestcase >= 0)
            {
                var testResult = CreateTestResult(indexOfNextTestcase);
                if (testResult != null)
                    testResults.Add(testResult);

                indexOfNextTestcase = FindIndexOfNextTestcase(indexOfNextTestcase + 1);
            }
            return testResults;
        }


        private TestResult CreateTestResult(int indexOfTestcase)
        {
            int currentLineIndex = indexOfTestcase;

            string line = _consoleOutput[currentLineIndex++];
            string qualifiedTestname = RemovePrefix(line).Trim();
            TestCase testCase = FindTestcase(qualifiedTestname);
            if (testCase == null)
            {
                _logger.DebugWarning(String.Format(Resources.NoKnownTestCaseMessage, line));
                return null;
            }

            if (currentLineIndex >= _consoleOutput.Count)
            {
                CrashedTestCase = testCase;
                return CreateFailedTestResult(testCase, TimeSpan.FromMilliseconds(0), CrashText, "");
            }

            line = _consoleOutput[currentLineIndex];
            SplitLineIfNecessary(ref line, currentLineIndex);
            currentLineIndex++;


            string errorMsg = "";
            while (!(IsFailedLine(line) || IsPassedLine(line)) && currentLineIndex <= _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = currentLineIndex < _consoleOutput.Count ? _consoleOutput[currentLineIndex] : "";
                SplitLineIfNecessary(ref line, currentLineIndex);
                currentLineIndex++;
            }
            if (IsFailedLine(line))
            {
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg);
                parser.Parse();
                return CreateFailedTestResult(testCase, ParseDuration(line), parser.ErrorMessage, parser.ErrorStackTrace);
            }
            if (IsPassedLine(line))
            {
                return CreatePassedTestResult(testCase, ParseDuration(line));
            }

            CrashedTestCase = testCase;
            string message = CrashText;
            message += errorMsg == "" ? "" : "\nTest output:\n\n" + errorMsg;
            return CreateFailedTestResult(testCase, TimeSpan.FromMilliseconds(0), message, "");
        }

        private void SplitLineIfNecessary(ref string line, int currentLineIndex)
        {
            Match testEndMatch = StreamingStandardOutputTestResultParser.PrefixedLineRegex.Match(line);
            if (testEndMatch.Success)
            {
                string restOfErrorMessage = testEndMatch.Groups[1].Value;
                string testEndPart = testEndMatch.Groups[2].Value;

                _consoleOutput.RemoveAt(currentLineIndex);
                _consoleOutput.Insert(currentLineIndex, testEndPart);
                _consoleOutput.Insert(currentLineIndex, restOfErrorMessage);

                line = restOfErrorMessage;
            }
        }

        private TimeSpan ParseDuration(string line)
        {
            return ParseDuration(line, _logger);
        }

        public static TimeSpan ParseDuration(string line, ILogger logger)
        {
            int durationInMs = 1;
            try
            {
                // duration is a 64-bit number (no decimals) in the user's locale
                int indexOfOpeningBracket = line.LastIndexOf('(');
                int lengthOfDurationPart = line.Length - indexOfOpeningBracket - 2;
                string durationPart = line.Substring(indexOfOpeningBracket + 1, lengthOfDurationPart);
                durationPart = durationPart.Replace("ms", "").Trim();
                durationInMs = int.Parse(durationPart, NumberStyles.Number);
            }
            catch (Exception)
            {
                logger.LogWarning(String.Format(Resources.ParseDurationMessage, line));
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

        private int FindIndexOfNextTestcase(int currentIndex)
        {
            while (currentIndex < _consoleOutput.Count)
            {
                string line = _consoleOutput[currentIndex];
                if (IsRunLine(line))
                {
                    return currentIndex;
                }
                currentIndex++;
            }
            return -1;
        }

        private TestCase FindTestcase(string qualifiedTestname)
        {
            return FindTestcase(qualifiedTestname, _testCasesRun);
        }

        public static TestCase FindTestcase(string qualifiedTestname, IList<TestCase> testCasesRun)
        {
            return testCasesRun.SingleOrDefault(tc => tc.FullyQualifiedName == qualifiedTestname);
        }

        public static bool IsRunLine(string line)
        {
            return line.StartsWith(Run, StringComparison.Ordinal);
        }

        public static bool IsPassedLine(string line)
        {
            return line.StartsWith(Passed, StringComparison.Ordinal);
        }

        public static bool IsFailedLine(string line)
        {
            return line.StartsWith(Failed, StringComparison.Ordinal);
        }

        public static string RemovePrefix(string line)
        {
            return line.Substring(Run.Length);
        }

    }

}