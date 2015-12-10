using System;

namespace GoogleTestAdapter.Model
{
    public enum TestOutcome2 { Passed, Failed, Skipped, None, NotFound }

    public class TestResult2
    {
        public TestCase2 TestCase { get; set; }
        public TestOutcome2 Outcome { get; set; }
        public string ComputerName { get; set; }
        public string DisplayName { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }

        public TestResult2(TestCase2 testCase)
        {
            TestCase = testCase;
        }

    }

}