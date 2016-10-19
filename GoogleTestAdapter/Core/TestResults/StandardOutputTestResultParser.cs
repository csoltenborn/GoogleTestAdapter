using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class StandardOutputTestResultParser
    {
        private const string Run = "[ RUN      ]";
        private const string Failed = "[  FAILED  ]";
        private const string Passed = "[       OK ]";

        public const string CrashText = "!! This test has probably CRASHED !!";


        public TestCase CrashedTestCase { get; private set; }

        private readonly List<string> _consoleOutput;
        private readonly List<TestCase> _testCasesRun;
        private readonly TestEnvironment _testEnvironment;
        private readonly string _baseDir;


        public StandardOutputTestResultParser(IEnumerable<TestCase> testCasesRun, IEnumerable<string> consoleOutput, TestEnvironment testEnvironment, string baseDir)
        {
            _consoleOutput = consoleOutput.ToList();
            _testCasesRun = testCasesRun.ToList();
            _testEnvironment = testEnvironment;
            _baseDir = baseDir;
        }


        public List<TestResult> GetTestResults()
        {
            var testResults = new List<TestResult>();
            int indexOfNextTestcase = FindIndexOfNextTestcase(0);
            while (indexOfNextTestcase >= 0)
            {
                testResults.Add(CreateTestResult(indexOfNextTestcase));
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

            if (currentLineIndex >= _consoleOutput.Count)
            {
                CrashedTestCase = testCase;
                return CreateFailedTestResult(testCase, TimeSpan.FromMilliseconds(0), CrashText, "");
            }

            line = _consoleOutput[currentLineIndex++];

            string errorMsg = "";
            while (!(IsFailedLine(line) || IsPassedLine(line)) && currentLineIndex <= _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = currentLineIndex < _consoleOutput.Count ? _consoleOutput[currentLineIndex] : "";
                currentLineIndex++;
            }
            if (IsFailedLine(line))
            {
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg, _baseDir);
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

        private TimeSpan ParseDuration(string line)
        {
            return ParseDuration(line, _testEnvironment);
        }

        public static TimeSpan ParseDuration(string line, TestEnvironment testEnvironment)
        {
            int durationInMs = 1;
            try
            {
                // TODO check format in gtest code, replace with regex
                int indexOfOpeningBracket = line.LastIndexOf('(');
                int lengthOfDurationPart = line.Length - indexOfOpeningBracket - 2;
                string durationPart = line.Substring(indexOfOpeningBracket + 1, lengthOfDurationPart);
                durationPart = durationPart.Replace("ms", "").Trim();
                durationInMs = int.Parse(durationPart);
            }
            catch (Exception)
            {
                testEnvironment.LogWarning("Could not parse duration in line '" + line + "'");
            }

            return TimeSpan.FromMilliseconds(Math.Max(1, durationInMs));
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
            return testCasesRun.Single(tc => tc.FullyQualifiedName == qualifiedTestname);
        }

        public static bool IsRunLine(string line)
        {
            return line.StartsWith(Run);
        }

        public static bool IsPassedLine(string line)
        {
            return line.StartsWith(Passed);
        }

        public static bool IsFailedLine(string line)
        {
            return line.StartsWith(Failed);
        }

        public static string RemovePrefix(string line)
        {
            return line.Substring(Run.Length);
        }

    }

}