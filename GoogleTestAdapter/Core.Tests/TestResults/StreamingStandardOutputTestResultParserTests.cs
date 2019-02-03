using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class StreamingStandardOutputTestResultParserTests : TestsBase
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_CompleteOutput_ParsedCorrectly()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                @"  Actual: 20",
                @"Expected: 1000",
                @"[  FAILED  ] TestMath.AddFails (3 ms)",
                @"[ RUN      ] TestMath.AddPasses",
                @"[       OK ] TestMath.AddPasses(0 ms)",
                @"[ RUN      ] TestMath.Crash",
                @"unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.",
                @"[  FAILED  ] TestMath.Crash(9 ms)",
                @"[----------] 3 tests from TestMath(26 ms total)",
                @"",
                @"[----------] Global test environment tear-down",
                @"[==========] 3 tests from 1 test case ran. (36 ms total)",
                @"[  PASSED  ] 1 test.",
                @"[  FAILED  ] 2 tests, listed below:",
                @"[  FAILED  ] TestMath.AddFails",
                @"[  FAILED  ] TestMath.Crash",
                @"",
                @" 2 FAILED TESTS",
                @"",
            };        
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(3);

            results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddFails");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[0]);
            results[0].ErrorMessage.Should().NotContain(StreamingStandardOutputTestResultParser.CrashText);
            results[0].Duration.Should().Be(TimeSpan.FromMilliseconds(3));
            results[0].ErrorStackTrace.Should()
                .Contain(
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp");

            results[1].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsPassed(results[1]);
            results[1].Duration.Should().Be(StreamingStandardOutputTestResultParser.ShortTestDuration);

            results[2].TestCase.FullyQualifiedName.Should().Be("TestMath.Crash");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[2]);
            results[2].ErrorMessage.Should().NotContain(StreamingStandardOutputTestResultParser.CrashText);
            results[2].Duration.Should().Be(TimeSpan.FromMilliseconds(9));

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithImmediateCrash_CorrectResultHasCrashText()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                @"  Actual: 20",
                @"Expected: 1000",
                @"[  FAILED  ] TestMath.AddFails (3 ms)",
                @"[ RUN      ] TestMath.AddPasses"
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(2);

            results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddFails");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[0]);
            results[0].ErrorMessage.Should().NotContain(StreamingStandardOutputTestResultParser.CrashText);
            results[0].Duration.Should().Be(TimeSpan.FromMilliseconds(3));
            results[0].ErrorStackTrace.Should().Contain(@"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp");

            results[1].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[1]);
            results[1].ErrorMessage.Should().Contain(StreamingStandardOutputTestResultParser.CrashText);
            results[1].ErrorMessage.Should().NotContain("Test output:");
            results[1].Duration.Should().Be(TimeSpan.FromMilliseconds(0));

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithCrashAfterErrorMessage_CorrectResultHasCrashText()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                @"  Actual: 20",
                @"Expected: 1000",
                @"[  FAILED  ] TestMath.AddFails (3 ms)",
                @"[ RUN      ] TestMath.AddPasses",
                @"[       OK ] TestMath.AddPasses(0 ms)",
                @"[ RUN      ] TestMath.Crash",
                @"unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.",
            };        
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(3);

            results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddFails");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[0]);
            results[0].ErrorMessage.Should().NotContain(StreamingStandardOutputTestResultParser.CrashText);
            results[0].Duration.Should().Be(TimeSpan.FromMilliseconds(3));
            results[0].ErrorStackTrace.Should().Contain(@"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp");

            results[1].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsPassed(results[1]);
            results[1].Duration.Should().Be(StreamingStandardOutputTestResultParser.ShortTestDuration);

            results[2].TestCase.FullyQualifiedName.Should().Be("TestMath.Crash");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[2]);
            results[2].ErrorMessage.Should().Contain(StreamingStandardOutputTestResultParser.CrashText);
            results[2].ErrorMessage.Should().Contain("Test output:");
            results[2].ErrorMessage.Should().Contain("unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.");
            results[2].Duration.Should().Be(TimeSpan.FromMilliseconds(0));

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithPrefixedPassedLine_PassingTestIsRecognized()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                @"  Actual: 20",
                @"Expected: 1000",
                @"[  FAILED  ] TestMath.AddFails (3 ms)",
                @"[ RUN      ] TestMath.AddPasses",
                @"DummyOutput[       OK ] TestMath.AddPasses(0 ms)",
                @"[ RUN      ] TestMath.Crash",
                @"unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.",
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(3);

            results[1].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsPassed(results[1]);
            results[1].Duration.Should().Be(StreamingStandardOutputTestResultParser.ShortTestDuration);

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithPrefixedFailedLine_FailingTestIsRecognized()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                @"  Actual: 20",
                @"Expected: 1000",
                @"[  FAILED  ] TestMath.AddFails (3 ms)",
                @"[ RUN      ] TestMath.AddPasses",
                @"DummyOutput[  FAILED  ] TestMath.AddPasses(0 ms)",
                @"[ RUN      ] TestMath.Crash",
                @"unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.",
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(3);

            results[1].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[1]);
            results[1].ErrorMessage.Should().Contain("DummyOutput");
            results[1].Duration.Should().Be(StreamingStandardOutputTestResultParser.ShortTestDuration);

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithInvalidDurationUnit_DefaultDurationIsUsedAndWarningIsProduced()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                @"  Actual: 20",
                @"Expected: 1000",
                @"[  FAILED  ] TestMath.AddFails (3 s)"
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().ContainSingle();
            results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddFails");
            results[0].Duration.Should().Be(TimeSpan.FromMilliseconds(1));
            results[0].ErrorStackTrace.Should().Contain(@"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp");

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains("'[  FAILED  ] TestMath.AddFails (3 s)'"))), Times.Exactly(1));

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithThousandsSeparatorInDuration_ParsedCorrectly()
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            try
            {
                string[] consoleOutput = {
                    @"[==========] Running 3 tests from 1 test case.",
                    @"[----------] Global test environment set-up.",
                    @"[----------] 3 tests from TestMath",
                    @"[ RUN      ] TestMath.AddFails",
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp(6): error: Value of: Add(10, 10)",
                    @"  Actual: 20",
                    @"Expected: 1000",
                    @"[  FAILED  ] TestMath.AddFails (4,656 ms)",
                };
                var cases = GetTestCases();

                var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
                consoleOutput.ToList().ForEach(parser.ReportLine);
                parser.Flush();
                IList<TestResult> results = parser.TestResults;

                results.Should().ContainSingle();
                results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddFails");
                results[0].Duration.Should().Be(TimeSpan.FromMilliseconds(4656));

                CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithConsoleOutput_ConsoleOutputIsIgnored()
        {
            string[] consoleOutput = {
                @"[==========] Running 1 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 1 tests from TestMath",
                @"[ RUN      ] TestMath.AddPasses",
                @"Some output produced by the exe",
                @"[       OK ] TestMath.AddPasses(0 ms)",
                @"[----------] 1 tests from TestMath(26 ms total)",
                @"",
                @"[----------] Global test environment tear-down",
                @"[==========] 3 tests from 1 test case ran. (36 ms total)",
                @"[  PASSED  ] 1 test.",
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().ContainSingle();
            results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsPassed(results[0]);

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithPrefixingTest_BothTestsAreFound()
        {
            string[] consoleOutput =
            {
                @"[==========] Running 2 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 2 tests from TestMath",
                @"[ RUN      ] Test.AB",
                @"[       OK ] Test.A(0 ms)",
                @"[ RUN      ] Test.A",
                @"[       OK ] Test.A(0 ms)",
                @"[----------] 2 tests from TestMath(26 ms total)",
                @"",
                @"[----------] Global test environment tear-down",
                @"[==========] 2 tests from 1 test case ran. (36 ms total)",
                @"[  PASSED  ] 2 test.",
            };
            var cases = new List<TestCase>
            {
                TestDataCreator.ToTestCase("Test.AB", TestDataCreator.DummyExecutable,
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp"),
                TestDataCreator.ToTestCase("Test.A", TestDataCreator.DummyExecutable,
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp")
            };

            var parser = new StreamingStandardOutputTestResultParser(cases, TestEnvironment.Logger, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            results.Should().HaveCount(2);
            results[0].TestCase.FullyQualifiedName.Should().Be("Test.AB");
            XmlTestResultParserTests.AssertTestResultIsPassed(results[0]);
            results[1].TestCase.FullyQualifiedName.Should().Be("Test.A");
            XmlTestResultParserTests.AssertTestResultIsPassed(results[1]);

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithSkippedTest_AllResultsAreFound()
        {
            string[] consoleOutput = @"[==========] Running 3 tests from 1 test suite.
[----------] Global test environment set-up.
[----------] 3 tests from Test
[ RUN      ] Test.Succeed
[       OK ] Test.Succeed (0 ms)
[ RUN      ] Test.Skip
[  SKIPPED ] Test.Skip (1 ms)
[ RUN      ] Test.Fail
C:\...\test.cpp(14): error: Value of: false
  Actual: false
Expected: true
[  FAILED  ] Test.Fail (0 ms)
[----------] 3 tests from Test (3 ms total)

[----------] Global test environment tear-down
[==========] 3 tests from 1 test suite ran. (6 ms total)
[  PASSED  ] 1 test.
[  SKIPPED ] 1 test, listed below:
[  SKIPPED ] Test.Skip
[  FAILED  ] 1 test, listed below:
[  FAILED  ] Test.Fail

 1 FAILED TEST
".Split('\n');
            var cases = new List<TestCase>
            {
                TestDataCreator.ToTestCase("Test.Succeed", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
                TestDataCreator.ToTestCase("Test.Skip", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
                TestDataCreator.ToTestCase("Test.Fail", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
            };

            var parser = new StreamingStandardOutputTestResultParser(cases, TestEnvironment.Logger, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            results.Should().HaveCount(3);

            var result = results[0];
            result.TestCase.FullyQualifiedName.Should().Be("Test.Succeed");
            XmlTestResultParserTests.AssertTestResultIsPassed(result);

            result = results[1];
            result.TestCase.FullyQualifiedName.Should().Be("Test.Skip");
            XmlTestResultParserTests.AssertTestResultIsSkipped(result);

            result = results[2];
            result.TestCase.FullyQualifiedName.Should().Be("Test.Fail");
            XmlTestResultParserTests.AssertTestResultIsFailure(result);

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithSkippedTestAsLastTest_AllResultsAreFound()
        {
            string[] consoleOutput = @"[==========] Running 3 tests from 1 test suite.
[----------] Global test environment set-up.
[----------] 3 tests from Test
[ RUN      ] Test.Succeed
[       OK ] Test.Succeed (0 ms)
[ RUN      ] Test.Fail
C:\...\test.cpp(14): error: Value of: false
  Actual: false
Expected: true
[  FAILED  ] Test.Fail (0 ms)
[ RUN      ] Test.Skip
[  SKIPPED ] Test.Skip (1 ms)
[----------] 3 tests from Test (3 ms total)

[----------] Global test environment tear-down
[==========] 3 tests from 1 test suite ran. (6 ms total)
[  PASSED  ] 1 test.
[  SKIPPED ] 1 test, listed below:
[  SKIPPED ] Test.Skip
[  FAILED  ] 1 test, listed below:
[  FAILED  ] Test.Fail

 1 FAILED TEST
".Split('\n');
            var cases = new List<TestCase>
            {
                TestDataCreator.ToTestCase("Test.Succeed", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
                TestDataCreator.ToTestCase("Test.Skip", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
                TestDataCreator.ToTestCase("Test.Fail", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
            };

            var parser = new StreamingStandardOutputTestResultParser(cases, TestEnvironment.Logger, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            results.Should().HaveCount(3);

            var result = results[0];
            result.TestCase.FullyQualifiedName.Should().Be("Test.Succeed");
            XmlTestResultParserTests.AssertTestResultIsPassed(result);

            result = results[1];
            result.TestCase.FullyQualifiedName.Should().Be("Test.Fail");
            XmlTestResultParserTests.AssertTestResultIsFailure(result);

            result = results[2];
            result.TestCase.FullyQualifiedName.Should().Be("Test.Skip");
            XmlTestResultParserTests.AssertTestResultIsSkipped(result);

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void OutputHandling_OutputManyLinesWithNewlines_IsParsedCorrectly()
        {
            var consoleOutput = File.ReadAllLines(TestResources.Tests_ReleaseX64_Output, Encoding.Default);
            var testCases = new GoogleTestDiscoverer(MockLogger.Object, MockOptions.Object)
                .GetTestsFromExecutable(TestResources.Tests_ReleaseX64);

            var parser = new StreamingStandardOutputTestResultParser(testCases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            var testResult = results.Single(tr => tr.DisplayName == "OutputHandling.Output_ManyLinesWithNewlines");
            var expectedErrorMessage =
                "before test 1\nbefore test 2\nExpected: 1\nTo be equal to: 2\ntest output\nafter test 1\nafter test 2";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void OutputHandling_OutputOneLineWithNewlines_IsParsedCorrectly()
        {
            var consoleOutput = File.ReadAllLines(TestResources.Tests_ReleaseX64_Output, Encoding.Default);
            var testCases = new GoogleTestDiscoverer(MockLogger.Object, MockOptions.Object)
                .GetTestsFromExecutable(TestResources.Tests_ReleaseX64);

            var parser = new StreamingStandardOutputTestResultParser(testCases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            var testResult = results.Single(tr => tr.DisplayName == "OutputHandling.Output_OneLineWithNewlines");
            var expectedErrorMessage =
                "before test\nExpected: 1\nTo be equal to: 2\ntest output\nafter test";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void OutputHandling_OutputOneLine_IsParsedCorrectly()
        {
            var consoleOutput = File.ReadAllLines(TestResources.Tests_ReleaseX64_Output, Encoding.Default);
            var testCases = new GoogleTestDiscoverer(MockLogger.Object, MockOptions.Object)
                .GetTestsFromExecutable(TestResources.Tests_ReleaseX64);

            var parser = new StreamingStandardOutputTestResultParser(testCases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            var testResult = results.Single(tr => tr.DisplayName == "OutputHandling.Output_OneLine");
            var expectedErrorMessage =
                "before test\nExpected: 1\nTo be equal to: 2\ntest output\nafter test";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void OutputHandling_ManyLinesWithNewlines_IsParsedCorrectly()
        {
            var consoleOutput = File.ReadAllLines(TestResources.Tests_ReleaseX64_Output, Encoding.Default);
            var testCases = new GoogleTestDiscoverer(MockLogger.Object, MockOptions.Object)
                .GetTestsFromExecutable(TestResources.Tests_ReleaseX64);

            var parser = new StreamingStandardOutputTestResultParser(testCases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            var testResult = results.Single(tr => tr.DisplayName == "OutputHandling.ManyLinesWithNewlines");
            var expectedErrorMessage =
                "before test 1\nbefore test 2\nExpected: 1\nTo be equal to: 2\nafter test 1\nafter test 2";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void OutputHandling_OneLineWithNewlines_IsParsedCorrectly()
        {
            var consoleOutput = File.ReadAllLines(TestResources.Tests_ReleaseX64_Output, Encoding.Default);
            var testCases = new GoogleTestDiscoverer(MockLogger.Object, MockOptions.Object)
                .GetTestsFromExecutable(TestResources.Tests_ReleaseX64);

            var parser = new StreamingStandardOutputTestResultParser(testCases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            var testResult = results.Single(tr => tr.DisplayName == "OutputHandling.Output_OneLineWithNewlines");
            var expectedErrorMessage =
                "before test\nExpected: 1\nTo be equal to: 2\ntest output\nafter test";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void OutputHandling_OneLine_IsParsedCorrectly()
        {
            var consoleOutput = File.ReadAllLines(TestResources.Tests_ReleaseX64_Output, Encoding.Default);
            var testCases = new GoogleTestDiscoverer(MockLogger.Object, MockOptions.Object)
                .GetTestsFromExecutable(TestResources.Tests_ReleaseX64);

            var parser = new StreamingStandardOutputTestResultParser(testCases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            var results = parser.TestResults;

            var testResult = results.Single(tr => tr.DisplayName == "OutputHandling.OneLine");
            var expectedErrorMessage =
                "before test\nExpected: 1\nTo be equal to: 2\nafter test";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        private List<TestCase> GetTestCases()
        {
            var cases = new List<TestCase>
            {
                TestDataCreator.ToTestCase("TestMath.AddFails", TestDataCreator.DummyExecutable,
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp"),
                TestDataCreator.ToTestCase("TestMath.Crash", TestDataCreator.DummyExecutable,
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp"),
                TestDataCreator.ToTestCase("TestMath.AddPasses", TestDataCreator.DummyExecutable,
                    @"c:\users\chris\documents\visual studio 2015\projects\consoleapplication1\consoleapplication1tests\source.cpp")
            };
            return cases;
        }

        private void CheckStandardOutputResultParser(IEnumerable<TestCase> testCasesRun, IEnumerable<string> consoleOutput, 
            IList<TestResult> results, TestCase crashedTestCase)
        {
            var parser = new StandardOutputTestResultParser(testCasesRun, consoleOutput, MockLogger.Object);

            parser.GetTestResults().Should().BeEquivalentTo(results);
            parser.CrashedTestCase.Should().Be(crashedTestCase);
        }
    }

}