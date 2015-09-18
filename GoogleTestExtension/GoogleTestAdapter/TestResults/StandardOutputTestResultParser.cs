using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestResults
{
    class StandardOutputTestResultParser
    {
        private const string Run    = "[ RUN      ]";
        private const string Failed = "[  FAILED  ]";
        private const string Passed = "[       OK ]";

        internal const string CrashText = "!! This is probably the test that crashed !!";

        private List<string> ConsoleOutput { get; }
        private List<TestCase> TestCasesRun { get; }
        private IMessageLogger Logger { get; }

        internal TestCase CrashedTestCase { get; private set; }

        internal StandardOutputTestResultParser(IEnumerable<string> consoleOutput, IEnumerable<TestCase> cases, IMessageLogger logger)
        {
            this.ConsoleOutput = consoleOutput.ToList();
            this.TestCasesRun = cases.ToList();
            this.Logger = logger;
        }

        internal List<TestResult> GetTestResults()
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
                return CreateFailedTestResult(testCase, true, CrashText, TimeSpan.FromMilliseconds(0));
            }

            line = ConsoleOutput[currentLineIndex++];

            if (IsPassedLine(line))
            {
                return CreatePassedTestResult(testCase, ParseDuration(line));
            }

            string errorMsg = "";
            while (!IsFailedLine(line) && currentLineIndex < ConsoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = ConsoleOutput[currentLineIndex++];
            }
            if (IsFailedLine(line))
            {
                return CreateFailedTestResult(testCase, false, errorMsg, ParseDuration(line));
            }

            string appendedMessage = errorMsg == "" ? "" : "\n\n" + errorMsg;
            return CreateFailedTestResult(testCase, true, CrashText + appendedMessage, TimeSpan.FromMilliseconds(0));
        }

        private TimeSpan ParseDuration(string line)
        {
            int durationInMs = 1;
            try
            {
                int indexOfOpeningBracket = line.LastIndexOf('(');
                int lengthOfDurationPart = line.Length - indexOfOpeningBracket - 2;
                string durationPart = line.Substring(indexOfOpeningBracket + 1, lengthOfDurationPart);
                if (durationPart.Contains("ms"))
                {
                    durationPart = durationPart.Replace("ms", "");
                    durationPart = durationPart.Trim();
                    durationInMs = int.Parse(durationPart);
                }
                if (durationPart.Contains("s"))
                {
                    durationPart = durationPart.Replace("s", "");
                    durationPart = durationPart.Trim();
                    durationInMs = int.Parse(durationPart) * 1000;
                }
            }
            catch (Exception)
            {
                Logger.SendMessage(TestMessageLevel.Warning, "Could not parse duration in line '" + line + "'");
            }

            return TimeSpan.FromMilliseconds(Math.Max(1, durationInMs));
        }

        private TestResult CreatePassedTestResult(TestCase testCase, TimeSpan duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = " ",
                Outcome = TestOutcome.Passed,
                ErrorMessage = "",
                Duration = duration
            };
        }

        private TestResult CreateFailedTestResult(TestCase testCase, bool crashed, string errorMessage, TimeSpan duration)
        {
            if (crashed)
            {
                CrashedTestCase = testCase;
            }
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = crashed ? "because it CRASHED!" : " ",
                Outcome = TestOutcome.Failed,
                ErrorMessage = errorMessage,
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