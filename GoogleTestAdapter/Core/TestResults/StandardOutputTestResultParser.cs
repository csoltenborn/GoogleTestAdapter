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

        private List<string> ConsoleOutput { get; }
        private List<TestCase> TestCasesRun { get; }
        private TestEnvironment TestEnvironment { get; }
        private string BaseDir { get; }


        public StandardOutputTestResultParser(IEnumerable<TestCase> testCasesRun, IEnumerable<string> consoleOutput, TestEnvironment testEnvironment, string baseDir)
        {
            this.ConsoleOutput = consoleOutput.ToList();
            this.TestCasesRun = testCasesRun.ToList();
            this.TestEnvironment = testEnvironment;
            BaseDir = baseDir;
        }


        public List<TestResult> GetTestResults()
        {
            List<TestResult> testResults = new List<TestResult>();
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

            string line = ConsoleOutput[currentLineIndex++];
            string qualifiedTestname = RemovePrefix(line).Trim();
            TestCase testCase = FindTestcase(qualifiedTestname);

            if (currentLineIndex >= ConsoleOutput.Count)
            {
                return CreateFailedTestResult(testCase, TimeSpan.FromMilliseconds(0), true, CrashText);
            }

            line = ConsoleOutput[currentLineIndex++];

            string errorMsg = "";
            while (!(IsFailedLine(line) || IsPassedLine(line)) && currentLineIndex < ConsoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = ConsoleOutput[currentLineIndex++];
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
                TestEnvironment.LogWarning("Could not parse duration in line '" + line + "'");
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

            ErrorMessageParser parser = new ErrorMessageParser(errorMessage, BaseDir);
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
            while (currentIndex < ConsoleOutput.Count)
            {
                string line = ConsoleOutput[currentIndex];
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
            return TestCasesRun.First(tc => tc.FullyQualifiedName.StartsWith(qualifiedTestname));
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