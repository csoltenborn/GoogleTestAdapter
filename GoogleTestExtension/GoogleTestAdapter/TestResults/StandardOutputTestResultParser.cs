using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    public class StandardOutputTestResultParser
    {
        private const string RUN    = "[ RUN      ]";
        private const string FAILED = "[  FAILED  ]";
        private const string PASSED = "[       OK ]";

        public const string CRASH_TEXT = "!! This is probably the test that crashed !!";

        private readonly List<string> ConsoleOutput;
        private readonly List<TestCase> TestCasesRun;
        private readonly IMessageLogger Logger;

        public TestCase CrashedTestCase { get; private set; }

        public StandardOutputTestResultParser(IEnumerable<string> consoleOutput, IEnumerable<TestCase> cases, IMessageLogger logger)
        {
            this.ConsoleOutput = consoleOutput.ToList();
            this.TestCasesRun = cases.ToList();
            this.Logger = logger;
        }

        public List<TestResult> GetTestResults()
        {
            List<TestResult> TestResults = new List<TestResult>();
            int IndexOfNextTestcase = FindIndexOfNextTestcase(0);
            while (IndexOfNextTestcase >= 0)
            {
                TestResults.Add(CreateTestResult(IndexOfNextTestcase));
                IndexOfNextTestcase = FindIndexOfNextTestcase(IndexOfNextTestcase + 1);
            }
            return TestResults;
        }

        private TestResult CreateTestResult(int indexOfTestcase)
        {
            int CurrentLineIndex = indexOfTestcase;

            string Line = ConsoleOutput[CurrentLineIndex++];
            string QualifiedTestname = RemovePrefix(Line).Trim();
            TestCase TestCase = FindTestcase(QualifiedTestname);

            if (CurrentLineIndex >= ConsoleOutput.Count)
            {
                return CreateFailedTestResult(TestCase, true, CRASH_TEXT, TimeSpan.FromMilliseconds(0));
            }

            Line = ConsoleOutput[CurrentLineIndex++];

            if (IsPassedLine(Line))
            {
                return CreatePassedTestResult(TestCase, ParseDuration(Line));
            }

            string ErrorMsg = "";
            while (!IsFailedLine(Line) && CurrentLineIndex < ConsoleOutput.Count)
            {
                ErrorMsg += Line + "\n";
                Line = ConsoleOutput[CurrentLineIndex++];
            }
            if (IsFailedLine(Line))
            {
                return CreateFailedTestResult(TestCase, false, ErrorMsg, ParseDuration(Line));
            }

            string AppendedMessage = ErrorMsg == "" ? "" : "\n\n" + ErrorMsg;
            return CreateFailedTestResult(TestCase, true, CRASH_TEXT + AppendedMessage, TimeSpan.FromMilliseconds(0));
        }

        private TimeSpan ParseDuration(string line)
        {
            int DurationInMs = 1;
            try
            {
                int IndexOfOpeningBracket = line.LastIndexOf('(');
                int LengthOfDurationPart = line.Length - IndexOfOpeningBracket - 2;
                string DurationPart = line.Substring(IndexOfOpeningBracket + 1, LengthOfDurationPart);
                if (DurationPart.Contains("ms"))
                {
                    DurationPart = DurationPart.Replace("ms", "");
                    DurationPart = DurationPart.Trim();
                    DurationInMs = int.Parse(DurationPart);
                }
                if (DurationPart.Contains("s"))
                {
                    DurationPart = DurationPart.Replace("s", "");
                    DurationPart = DurationPart.Trim();
                    DurationInMs = int.Parse(DurationPart) * 1000;
                }
            }
            catch (Exception)
            {
                Logger.SendMessage(TestMessageLevel.Warning, "Could not parse duration in line '" + line + "'");
            }

            return TimeSpan.FromMilliseconds(Math.Max(1, DurationInMs));
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
                string Line = ConsoleOutput[currentIndex];
                if (IsRunLine(Line))
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
            return line.StartsWith(RUN);
        }

        private bool IsPassedLine(string line)
        {
            return line.StartsWith(PASSED);
        }

        private bool IsFailedLine(string line)
        {
            return line.StartsWith(FAILED);
        }

        private string RemovePrefix(string line)
        {
            return line.Substring(RUN.Length);
        }

    }

}