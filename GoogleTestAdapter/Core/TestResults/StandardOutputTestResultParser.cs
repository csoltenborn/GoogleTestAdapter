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

        public const string CrashText = "!! This is probably the test that crashed !!";


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
                return CreateFailedTestResult(testCase, TimeSpan.FromMilliseconds(0), true, CrashText);
            }

            line = _consoleOutput[currentLineIndex++];

            string errorMsg = "";
            while (!(IsFailedLine(line) || IsPassedLine(line)) && currentLineIndex < _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = _consoleOutput[currentLineIndex++];
            }
            if (IsFailedLine(line))
            {
                return CreateFailedTestResult(testCase, ParseDuration(line), false, errorMsg);
            }
            if (IsPassedLine(line))
            {
                return CreatePassedTestResult(testCase, ParseDuration(line));
            }

            string appendedMessage = errorMsg == "" ? "" : "\n\n" + errorMsg;
            return CreateFailedTestResult(testCase, TimeSpan.FromMilliseconds(0), true, CrashText + appendedMessage);
        }

        private TimeSpan ParseDuration(string line)
        {
            int durationInMs = 1;
            try
            {
                int indexOfOpeningBracket = line.LastIndexOf('(');
                int lengthOfDurationPart = line.Length - indexOfOpeningBracket - 2;
                string durationPart = line.Substring(indexOfOpeningBracket + 1, lengthOfDurationPart);
                durationPart = durationPart.Replace("ms", "").Trim();
                durationInMs = int.Parse(durationPart);
            }
            catch (Exception)
            {
                _testEnvironment.LogWarning("Could not parse duration in line '" + line + "'");
            }

            return TimeSpan.FromMilliseconds(Math.Max(1, durationInMs));
        }

        private TestResult CreatePassedTestResult(TestCase testCase, TimeSpan duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Passed,
                ErrorMessage = "",
                Duration = duration
            };
        }

        private TestResult CreateFailedTestResult(TestCase testCase, TimeSpan duration, bool crashed, string errorMessage)
        {
            if (crashed)
            {
                CrashedTestCase = testCase;
            }

            var parser = new ErrorMessageParser(errorMessage, _baseDir);
            parser.Parse();
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName,
                Outcome = TestOutcome.Failed,
                ErrorMessage = crashed ? CrashText : parser.ErrorMessage,
                ErrorStackTrace = parser.ErrorStackTrace,
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
            return _testCasesRun.First(tc => tc.FullyQualifiedName.StartsWith(qualifiedTestname));
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