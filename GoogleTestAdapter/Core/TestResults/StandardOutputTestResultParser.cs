using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class StandardOutputTestResultParser
    {
        private class DummyTestFrameworkReporter : ITestFrameworkReporter
        {
            public void ReportTestResults(IEnumerable<TestResult> testResults)
            {
            }

            public void ReportTestsFound(IEnumerable<TestCase> testCases)
            {
            }

            public void ReportTestsStarted(IEnumerable<TestCase> testCases)
            {
            }
        }

        public TestCase CrashedTestCase { get; private set; }

        private readonly List<string> _consoleOutput;
        private readonly List<TestCase> _testCasesRun;
        private readonly ILogger _logger;

        public StandardOutputTestResultParser(IEnumerable<TestCase> testCasesRun, IEnumerable<string> consoleOutput, ILogger logger)
        {
            _consoleOutput = consoleOutput.ToList();
            _testCasesRun = testCasesRun.ToList();
            _logger = logger;
        }

        public IList<TestResult> GetTestResults()
        {
            var streamingParser = new StreamingStandardOutputTestResultParser(_testCasesRun, _logger, new DummyTestFrameworkReporter());
            _consoleOutput.ForEach(l => streamingParser.ReportLine(l));
            streamingParser.Flush();

            CrashedTestCase = streamingParser.CrashedTestCase;
            return streamingParser.TestResults;
        }
    }

}