using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    public class GoogleTestResultStandardOutputParser
    {

        private const string RUN    = "[ RUN      ]";
        private const string FAILED = "[  FAILED  ]";
        private const string PASSED = "[       OK ]";

        public const string CRASH_TEXT = "!! This is probably the test that crashed !!";

        private readonly List<string> ConsoleOutput;
        private readonly List<TestCase> Cases;
        private readonly IMessageLogger Logger;

        public TestCase CrashedTestCase { get; private set; }

        public GoogleTestResultStandardOutputParser(IEnumerable<string> consoleOutput, IEnumerable<TestCase> cases, IMessageLogger logger)
        {
            this.ConsoleOutput = consoleOutput.ToList();
            this.Cases = cases.ToList();
            this.Logger = logger;
        }

        public List<TestResult> GetTestResults()
        {
            List<TestResult> results = new List<TestResult>();
            int IndexOfNextTestcase = FindIndexOfNextTestcase(0);
            while (IndexOfNextTestcase >= 0)
            {
                results.Add(CreateTestResult(IndexOfNextTestcase));
                IndexOfNextTestcase = FindIndexOfNextTestcase(IndexOfNextTestcase + 1);
            }
            return results;
        }

        private TestResult CreateTestResult(int indexOfTestcase)
        {
            int CurrentLineIndex = indexOfTestcase;

            string Line = ConsoleOutput[CurrentLineIndex++];
            string QualifiedTestname = RemovePrefix(Line).Trim();
            TestCase testcase = FindTestcase(QualifiedTestname);

            if (CurrentLineIndex >= ConsoleOutput.Count)
            {
                return CreateFailedTestResult(testcase, CRASH_TEXT, TimeSpan.FromMilliseconds(0), true);
            }

            Line = ConsoleOutput[CurrentLineIndex++];

            if (IsPassedLine(Line))
            {
                return CreatePassedTestResult(testcase, ParseDuration(Line));
            }
            else 
            {
                string ErrorMsg = "";
                while (!IsFailedLine(Line) && CurrentLineIndex < ConsoleOutput.Count)
                {
                    ErrorMsg += Line + "\n";
                    Line = ConsoleOutput[CurrentLineIndex++];
                }
                if (IsFailedLine(Line))
                {
                    return CreateFailedTestResult(testcase, ErrorMsg, ParseDuration(Line), false);
                }
                else
                {
                    string AppendedMessage = ErrorMsg == "" ? "" : "\n\n" + ErrorMsg;
                    return CreateFailedTestResult(testcase, CRASH_TEXT + AppendedMessage, TimeSpan.FromMilliseconds(0), true);
                }
            }

        }

        private TimeSpan ParseDuration(string line)
        {
            string TheString = line;
            int Result = 0;
            try
            {
                int indexOpeningBracket = line.LastIndexOf('(');
                int length = line.Length - indexOpeningBracket - 2;
                TheString = line.Substring(indexOpeningBracket + 1, length);
                if (TheString.Contains("ms"))
                {
                    TheString = TheString.Replace("ms", "");
                    TheString = TheString.Trim();
                    Result = int.Parse(TheString);
                }
                if (TheString.Contains("s"))
                {
                    TheString = TheString.Replace("s", "");
                    TheString = TheString.Trim();
                    Result = int.Parse(TheString) * 1000;
                }
            }
            catch (Exception)
            {
                Logger.SendMessage(TestMessageLevel.Warning, "Could not parse duration in line '" + line + "'");
            }
            return TimeSpan.FromMilliseconds(Math.Max(1, Result));
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

        private TestResult CreateFailedTestResult(TestCase testCase, string errorMessage, TimeSpan duration, bool crashed)
        {
            string TheDisplayName = !crashed ? " " : "because it CRASHED!";
            if (crashed)
            {
                CrashedTestCase = testCase;
            }
            return new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = TheDisplayName,
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
            return Cases.First(tc => tc.FullyQualifiedName.StartsWith(qualifiedTestname));
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
