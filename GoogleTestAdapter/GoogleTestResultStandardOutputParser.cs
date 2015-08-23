using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System;

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

        public GoogleTestResultStandardOutputParser(IEnumerable<string> consoleOutput, IEnumerable<TestCase> cases)
        {
            this.ConsoleOutput = consoleOutput.ToList();
            this.Cases = cases.ToList();
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
                return CreateFailedTestResult(testcase, CRASH_TEXT, 0);
            }

            Line = ConsoleOutput[CurrentLineIndex++];

            if (IsPassedLine(Line))
            {
                return CreatePassedTestResult(testcase, ParseDuration(Line, QualifiedTestname));
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
                    return CreateFailedTestResult(testcase, ErrorMsg, ParseDuration(Line, QualifiedTestname));
                }
                else
                {
                    string AppendedMessage = ErrorMsg == "" ? "" : "\n\n" + ErrorMsg;
                    return CreateFailedTestResult(testcase, CRASH_TEXT + AppendedMessage, 0);
                }
            }

        }

        private int ParseDuration(string line, string qualifiedName)
        {
            line = RemovePrefix(line).Replace(qualifiedName, "").Trim().Substring(1, line.Length - 2);
            if (line.EndsWith("ms"))
            {
                line = line.Replace("ms", "").Trim();
                return int.Parse(line);
            }
            if (line.Contains("s"))
            {
                line = line.Replace("s", "").Trim();
                return int.Parse(line) * 1000;
            }
            throw new Exception("Can't deal with duration: " + line);
        }

        private TestResult CreatePassedTestResult(TestCase testCase, int duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = System.Environment.MachineName,
                Outcome = TestOutcome.Passed,
                ErrorMessage = "",
                Duration = TimeSpan.FromMilliseconds(duration)
            };
        }

        private TestResult CreateFailedTestResult(TestCase testCase, string errorMessage, int duration)
        {
            return new TestResult(testCase)
            {
                ComputerName = System.Environment.MachineName,
                Outcome = TestOutcome.Failed,
                ErrorMessage = errorMessage,
                Duration = TimeSpan.FromMilliseconds(duration)
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
