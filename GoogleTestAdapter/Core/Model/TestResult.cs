using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.Model
{
    public enum TestOutcome { Passed, Failed, Skipped, None, NotFound }

    public class TestResultMessage
    {

    }

    public class TestResult
    {
        public TestCase TestCase { get; set; }

        public string ComputerName { get; set; }
        public string DisplayName { get; set; }

        public TestOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }

        public IList<TestResultMessage> Messages { get; } = new List<TestResultMessage>();

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