using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestResults
{
    public class TestResultSplitter
    {
        public TestCase CrashedTestCase { get; private set; }
        public IList<TestResult> TestResults { get; } = new List<TestResult>();

        private readonly List<TestCase> _testCasesRun;
        private readonly TestEnvironment _testEnvironment;
        private readonly string _baseDir;
        private readonly ITestFrameworkReporter _reporter;

        private readonly List<string> _consoleOutput = new List<string>();

        public TestResultSplitter(IEnumerable<TestCase> testCasesRun,
            TestEnvironment testEnvironment, string baseDir, ITestFrameworkReporter reporter)
        {
            _testCasesRun = testCasesRun.ToList();
            _testEnvironment = testEnvironment;
            _baseDir = baseDir;
            _reporter = reporter;
        }

        public void ReportLine(string line)
        {
            if (StandardOutputTestResultParser.IsRunLine(line))
            {
                if (_consoleOutput.Count > 0)
                    ReportTestResult();
                ReportTestStart(line);
            }
            _consoleOutput.Add(line);
        }

        public void Flush()
        {
            if (_consoleOutput.Count > 0)
                ReportTestResult();
        }

        private void ReportTestStart(string line)
        {
            string qualifiedTestname = StandardOutputTestResultParser.RemovePrefix(line).Trim();
            TestCase testCase = StandardOutputTestResultParser.FindTestcase(qualifiedTestname, _testCasesRun);
            _reporter.ReportTestsStarted(testCase.Yield());
        }

        private void ReportTestResult()
        {
            TestResult result = CreateTestResult();
            if (result != null)
            {
                TestResults.Add(result);
                _reporter.ReportTestResults(result.Yield());
            }
            _consoleOutput.Clear();
        }

        private TestResult CreateTestResult()
        {
            int currentLineIndex = 0;
            while (currentLineIndex < _consoleOutput.Count && 
                !StandardOutputTestResultParser.IsRunLine(_consoleOutput[currentLineIndex]))
                currentLineIndex++;
            if (currentLineIndex >= _consoleOutput.Count)
                return null;

            string line = _consoleOutput[currentLineIndex++];
            string qualifiedTestname = StandardOutputTestResultParser.RemovePrefix(line).Trim();
            TestCase testCase = StandardOutputTestResultParser.FindTestcase(qualifiedTestname, _testCasesRun);

            if (currentLineIndex >= _consoleOutput.Count)
            {
                CrashedTestCase = testCase;
                return StandardOutputTestResultParser.CreateFailedTestResult(
                    testCase, 
                    TimeSpan.FromMilliseconds(0), 
                    StandardOutputTestResultParser.CrashText, 
                    "");
            }

            line = _consoleOutput[currentLineIndex++];

            string errorMsg = "";
            while (
                !(StandardOutputTestResultParser.IsFailedLine(line) 
                    || StandardOutputTestResultParser.IsPassedLine(line)) 
                && currentLineIndex <= _consoleOutput.Count)
            {
                errorMsg += line + "\n";
                line = currentLineIndex < _consoleOutput.Count ? _consoleOutput[currentLineIndex] : "";
                currentLineIndex++;
            }
            if (StandardOutputTestResultParser.IsFailedLine(line))
            {
                ErrorMessageParser parser = new ErrorMessageParser(errorMsg, _baseDir);
                parser.Parse();
                return StandardOutputTestResultParser.CreateFailedTestResult(
                    testCase, 
                    StandardOutputTestResultParser.ParseDuration(line, _testEnvironment), 
                    parser.ErrorMessage, 
                    parser.ErrorStackTrace);
            }
            if (StandardOutputTestResultParser.IsPassedLine(line))
            {
                return StandardOutputTestResultParser.CreatePassedTestResult(
                    testCase, 
                    StandardOutputTestResultParser.ParseDuration(line, _testEnvironment));
            }

            CrashedTestCase = testCase;
            string message = StandardOutputTestResultParser.CrashText;
            message += errorMsg == "" ? "" : $"\nTest output:\n\n{errorMsg}";
            TestResult result = StandardOutputTestResultParser.CreateFailedTestResult(
                testCase, 
                TimeSpan.FromMilliseconds(0), 
                message, 
                "");
            return result;
        }

    }

}