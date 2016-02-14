using System;

namespace GoogleTestAdapter.Model
{
    public enum TestOutcome { Passed, Failed, Skipped, None, NotFound }

    public class TestResult
    {
        public TestCase TestCase { get; set; }

        public string ComputerName { get; set; }
        public string DisplayName { get; set; }

        public TestOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorStackTrace { get; set; }
        public TimeSpan Duration { get; set; }

        public TestResult(TestCase testCase)
        {
            TestCase = testCase;
        }

        public override string ToString()
        {
            return $"{DisplayName} ({Outcome})";
        }

    }

}