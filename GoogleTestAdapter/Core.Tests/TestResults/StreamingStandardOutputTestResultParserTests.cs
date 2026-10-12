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
        public void GetTestResults_FailingSetUpAndTearDownTestSuite_TestsFailAndTearDownTestSuiteTestCaseFails()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 2 test suites.",
                @"[----------] Global test environment set-up.",
                @"[----------] 2 tests from FailingSetUp",
                @"C:\tests\fixtures.cpp(5): error: Failed",
                @"setup failed",
                @"",
                @"[ RUN      ] FailingSetUp.First",
                @"C:\tests\fixtures.cpp(7): Skipped",
                @"",
                @"[  SKIPPED ] FailingSetUp.First (0 ms)",
                @"[ RUN      ] FailingSetUp.Second",
                @"C:\tests\fixtures.cpp(8): Skipped",
                @"",
                @"[  SKIPPED ] FailingSetUp.Second (0 ms)",
                @"[----------] 2 tests from FailingSetUp (1 ms total)",
                @"",
                @"[----------] 1 test from FailingTearDown",
                @"[ RUN      ] FailingTearDown.Only",
                @"[       OK ] FailingTearDown.Only (0 ms)",
                @"C:\tests\fixtures.cpp(12): error: Failed",
                @"teardown failed",
                @"",
                @"[----------] 1 test from FailingTearDown (0 ms total)",
                @"",
                @"[----------] Global test environment tear-down",
                @"[==========] 3 tests from 2 test suites ran. (2 ms total)",
                @"[  PASSED  ] 1 test.",
                @"[  SKIPPED ] 2 tests, listed below:",
                @"[  SKIPPED ] FailingSetUp.First",
                @"[  SKIPPED ] FailingSetUp.Second",
                @"[  FAILED  ] FailingSetUp: SetUpTestSuite or TearDownTestSuite",
                @"[  FAILED  ] FailingTearDown: SetUpTestSuite or TearDownTestSuite",
            };
            var cases = CreateFixtureTestCases("FailingSetUp.First", "FailingSetUp.Second", "FailingTearDown.Only");
            cases.Add(CreateTearDownTestSuiteTestCase("FailingTearDown", 1, 3));

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(4);
            foreach (string testName in new[] { "FailingSetUp.First", "FailingSetUp.Second" })
            {
                TestResult result = results.Single(r => r.TestCase.FullyQualifiedName == testName);
                result.Outcome.Should().Be(TestOutcome.Failed, testName);
                result.ErrorMessage.Should().StartWith("SetUpTestSuite() failed:").And.Contain("setup failed");
                result.ErrorStackTrace.Should().Contain(@"at FailingSetUp.SetUpTestSuite in C:\tests\fixtures.cpp:line 5");
            }
            results.Single(r => r.TestCase.FullyQualifiedName == "FailingTearDown.Only").Outcome.Should().Be(TestOutcome.Passed);
            TestResult tearDownResult = results.Single(r => r.TestCase.FullyQualifiedName == "FailingTearDown.TearDownTestSuite");
            tearDownResult.Outcome.Should().Be(TestOutcome.Failed);
            tearDownResult.ErrorMessage.Should().Contain("teardown failed");
            tearDownResult.ErrorStackTrace.Should().Contain(@"at FailingTearDown.TearDownTestSuite in C:\tests\fixtures.cpp:line 12");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_SkippingSetUpTestSuiteAndPassingTearDownTestSuite_TestIsSkippedWithMessageAndTearDownPasses()
        {
            string[] consoleOutput = {
                @"[----------] 1 test from SkippingSetUp",
                @"C:\tests\fixtures.cpp(18): Skipped",
                @"skipped in setup",
                @"",
                @"[ RUN      ] SkippingSetUp.Skipped",
                @"C:\tests\fixtures.cpp(20): Skipped",
                @"",
                @"[  SKIPPED ] SkippingSetUp.Skipped (0 ms)",
                @"[----------] 1 test from SkippingSetUp (0 ms total)",
                @"",
                @"[----------] 1 test from TypedFixture/0, where TypeParam = int",
                @"[ RUN      ] TypedFixture/0.Passes",
                @"[       OK ] TypedFixture/0.Passes (0 ms)",
                @"[----------] 1 test from TypedFixture/0 (0 ms total)",
            };
            var cases = CreateFixtureTestCases("SkippingSetUp.Skipped", "TypedFixture/0.Passes");
            cases.Add(CreateTearDownTestSuiteTestCase("TypedFixture/0", 1, 2));

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(3);
            TestResult skippedResult = results.Single(r => r.TestCase.FullyQualifiedName == "SkippingSetUp.Skipped");
            skippedResult.Outcome.Should().Be(TestOutcome.Skipped);
            skippedResult.ErrorMessage.Should().Be("skipped in setup");
            results.Single(r => r.TestCase.FullyQualifiedName == "TypedFixture/0.Passes").Outcome.Should().Be(TestOutcome.Passed);
            results.Single(r => r.TestCase.FullyQualifiedName == "TypedFixture/0.TearDownTestSuite").Outcome.Should().Be(TestOutcome.Passed);
        }

        private List<TestCase> CreateFixtureTestCases(params string[] qualifiedNames)
        {
            return TestDataCreator.CreateDummyTestCases(qualifiedNames).ToList();
        }

        private TestCase CreateTearDownTestSuiteTestCase(string suite, int nrOfTestCasesInSuite, int nrOfTestCasesInExecutable)
        {
            TestCase testCase = TestDataCreator.ToTestCase($"{suite}.{GoogleTestConstants.TearDownTestSuiteTestName}");
            testCase.Properties.Add(new TestCaseMetaDataProperty(nrOfTestCasesInSuite, nrOfTestCasesInExecutable, isTearDownTestSuite: true));
            return testCase;
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
            results[1].ErrorMessage.Should().NotContain("exit code");
            results[1].Duration.Should().Be(TimeSpan.FromMilliseconds(0));

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithImmediateCrashAndExitCode_CrashTextContainsExitCode()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddPasses"
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush(-1073741819);
            IList<TestResult> results = parser.TestResults;

            results.Should().ContainSingle();
            results[0].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[0]);
            results[0].ErrorMessage.Should().Be(StreamingStandardOutputTestResultParser.CrashText + " (exit code -1073741819, i.e. 0xC0000005)");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithImmediateCrash_StackTracePointsToCrashedTest()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddPasses"
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush(-1073741819);
            IList<TestResult> results = parser.TestResults;

            results.Should().ContainSingle();
            results[0].ErrorStackTrace.Should().Be(
                $@"at TestMath.AddPasses in {cases[2].CodeFilePath}:line {cases[2].LineNumber}{Environment.NewLine}");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithCrashOfTestWithoutSourceLocation_StackTraceIsEmpty()
        {
            string[] consoleOutput = {
                @"[==========] Running 1 test from 1 test case.",
                @"[----------] 1 test from TestMath",
                @"[ RUN      ] TestMath.Crash"
            };
            var cases = new List<TestCase> { TestDataCreator.ToTestCase("TestMath.Crash", TestDataCreator.DummyExecutable) };

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.TestResults.Should().ContainSingle();
            parser.TestResults[0].ErrorStackTrace.Should().BeEmpty();
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
            results[2].ErrorMessage.Should().Be(StreamingStandardOutputTestResultParser.CrashText);
            results[2].StandardOutput.Should().Be("unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.\n");
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
        public void GetTestResults_OutputWithConsoleOutput_ConsoleOutputIsStandardOutputOfResult()
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
            results[0].StandardOutput.Should().Be("Some output produced by the exe\n");

            CheckStandardOutputResultParser(cases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithEmptyExitCode_NoExitCodeOutputIsParsed()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin,
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputEnd,
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeOutput.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithExitCode_ExitCodeOutputIsParsed()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin,
                @"Some test output",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputEnd,
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeOutput.Should().BeEquivalentTo("Some test output");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithTwoLinesExitCode_ExitCodeOutputIsParsed()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin,
                "Output 1",
                "Output 2",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputEnd,
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeOutput.Should().BeEquivalentTo("Output 1", "Output 2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithTwoLinesExitCodeAndAdditionalOutput_ExitCodeOutputIsParsed()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin,
                "Output 1",
                "Output 2",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputEnd,
                "Some more output"
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeOutput.Should().BeEquivalentTo("Output 1", "Output 2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithNoLinesExitCodeAndNoEnd_NoExitCodeOutputIsParsed()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeOutput.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithTwoLinesExitCodeAndNoEnd_ExitCodeOutputIsParsed()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin,
                "Output 1",
                "Output 2",
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeOutput.Should().BeEquivalentTo("Output 1", "Output 2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithSkip_ExitCodeSkipIsTrue()
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
                @"",
                StreamingStandardOutputTestResultParser.GtaExitCodeSkip,
                StreamingStandardOutputTestResultParser.GtaExitCodeOutputBegin,
                "Output 1",
                "Output 2",
            };        
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.ExitCodeSkip.Should().BeTrue();
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
        public void GetTestResults_OutputWithSkippedTestWithMessage_MessageIsErrorMessageOfResult()
        {
            string[] consoleOutput = {
                @"[==========] Running 1 test from 1 test suite.",
                @"[----------] 1 test from Test",
                @"[ RUN      ] Test.Skip",
                @"C:\...\test.cpp(9): Skipped",
                @"Not supported on this platform",
                @"[  SKIPPED ] Test.Skip (0 ms)",
                @"[----------] 1 test from Test (1 ms total)",
            };
            var cases = new List<TestCase>
            {
                TestDataCreator.ToTestCase("Test.Skip", TestDataCreator.DummyExecutable, @"c:\somepath\source.cpp"),
            };

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            parser.Flush();

            parser.TestResults.Should().ContainSingle();
            parser.TestResults[0].Outcome.Should().Be(TestOutcome.Skipped);
            parser.TestResults[0].ErrorMessage.Should().Be("Not supported on this platform");
            parser.TestResults[0].ErrorStackTrace.Should().Be($@"at Test.Skip in C:\...\test.cpp:line 9{Environment.NewLine}");
            parser.TestResults[0].StandardOutput.Should().Be("C:\\...\\test.cpp(9): Skipped\nNot supported on this platform\n");

            CheckStandardOutputResultParser(cases, consoleOutput, parser.TestResults, parser.CrashedTestCase);
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
                "Expected: 1\nTo be equal to: 2\ntest output\nafter test 1\nafter test 2";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);
            testResult.StandardOutput.Should().Be(
                "before test 1\nbefore test 2\n" +
                "c:\\users\\chris\\git\\googletestadapter\\sampletests\\tests\\basictests.cpp(113): error:       Expected: 1\n" +
                "To be equal to: 2\ntest output\nafter test 1\nafter test 2\n");

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
                "Expected: 1\nTo be equal to: 2\ntest output\nafter test";
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
                "Expected: 1\nTo be equal to: 2\ntest output\nafter test";
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
                "Expected: 1\nTo be equal to: 2\nafter test 1\nafter test 2";
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
                "Expected: 1\nTo be equal to: 2\ntest output\nafter test";
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
                "Expected: 1\nTo be equal to: 2\nafter test";
            testResult.ErrorMessage.Should().Be(expectedErrorMessage);

            CheckStandardOutputResultParser(testCases, consoleOutput, results, parser.CrashedTestCase);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetTestResults_OutputWithTimedOutTest_ResultHasTimeoutTextAndNoTestIsCrashed()
        {
            string[] consoleOutput = {
                @"[==========] Running 3 tests from 1 test case.",
                @"[----------] Global test environment set-up.",
                @"[----------] 3 tests from TestMath",
                @"[ RUN      ] TestMath.AddFails",
                @"[  FAILED  ] TestMath.AddFails (3 ms)",
                @"[ RUN      ] TestMath.AddPasses",
                @"some output"
            };
            var cases = GetTestCases();

            var parser = new StreamingStandardOutputTestResultParser(cases, MockLogger.Object, MockFrameworkReporter.Object);
            consoleOutput.ToList().ForEach(parser.ReportLine);
            TestCase runningTestCase = parser.GetRunningTestCase(out TimeSpan runningFor);
            runningTestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            runningFor.Should().BeGreaterOrEqualTo(TimeSpan.Zero);

            parser.SetTimedOut(runningTestCase, TimeSpan.FromSeconds(2));
            parser.Flush();
            IList<TestResult> results = parser.TestResults;

            results.Should().HaveCount(2);
            results[1].TestCase.FullyQualifiedName.Should().Be("TestMath.AddPasses");
            XmlTestResultParserTests.AssertTestResultIsFailure(results[1]);
            results[1].ErrorMessage.Should().StartWith(StreamingStandardOutputTestResultParser.CreateTimeoutText(TimeSpan.FromSeconds(2)));
            results[1].ErrorMessage.Should().NotContain(StreamingStandardOutputTestResultParser.CrashText);
            results[1].ErrorMessage.Should().NotContain("some output");
            results[1].StandardOutput.Should().Be("some output\n");
            results[1].Duration.Should().Be(TimeSpan.FromSeconds(2));
            parser.TimedOutTestCase.Should().Be(runningTestCase);
            parser.CrashedTestCase.Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetRunningTestCase_AfterTestHasFinished_ReturnsNull()
        {
            var parser = new StreamingStandardOutputTestResultParser(GetTestCases(), MockLogger.Object, MockFrameworkReporter.Object);

            parser.ReportLine(@"[ RUN      ] TestMath.AddPasses");
            parser.GetRunningTestCase(out _).FullyQualifiedName.Should().Be("TestMath.AddPasses");
            parser.ReportLine(@"[       OK ] TestMath.AddPasses (0 ms)");

            parser.GetRunningTestCase(out TimeSpan runningFor).Should().BeNull();
            runningFor.Should().Be(TimeSpan.Zero);
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