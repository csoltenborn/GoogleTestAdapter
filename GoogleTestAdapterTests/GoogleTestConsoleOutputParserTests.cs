using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestConsoleOutputParserTests
    {

        private readonly string[] CONSOLE_OUTPUT_1 = {
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

        private readonly string[] CONSOLE_OUTPUT_2 = {
            @"[       OK ] TestMath.AddPasses(0 ms)",
            @"[ RUN      ] TestMath.Crash",
            @"unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.",
        };

        private readonly string[] CONSOLE_OUTPUT_3 = {
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

        private List<string> CrashesImmediately;
        private List<string> CrashesAfterErrorMsg;
        private List<string> Complete;

        [TestInitialize]
        public void SetUp()
        {
            CrashesImmediately = new List<string>(CONSOLE_OUTPUT_1);

            CrashesAfterErrorMsg = new List<string>(CONSOLE_OUTPUT_1);
            CrashesAfterErrorMsg.AddRange(CONSOLE_OUTPUT_2);

            Complete = new List<string>(CrashesAfterErrorMsg);
            Complete.AddRange(CONSOLE_OUTPUT_3);
        }

        [TestMethod]
        public void TestCompleteOutput()
        {
            List<TestResult> results = ComputeResults(Complete);

            Assert.AreEqual(3, results.Count);

            Assert.AreEqual("TestMath.AddFails", results[0].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[0].Outcome);
            Assert.IsFalse(results[0].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));

            Assert.AreEqual("TestMath.AddPasses", results[1].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Passed, results[1].Outcome);
            Assert.IsFalse(results[1].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));

            Assert.AreEqual("TestMath.Crash", results[2].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[2].Outcome);
            Assert.IsFalse(results[2].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));
        }

        [TestMethod]
        public void TestOutputWithImmediateCrash()
        {
            List<TestResult> results = ComputeResults(CrashesImmediately);

            Assert.AreEqual(2, results.Count);

            Assert.AreEqual("TestMath.AddFails", results[0].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[0].Outcome);
            Assert.IsFalse(results[0].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));

            Assert.AreEqual("TestMath.AddPasses", results[1].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[1].Outcome);
            Assert.IsTrue(results[1].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));
        }

        [TestMethod]
        public void TestOutputWithCrashAfterErrorMessage()
        {
            List<TestResult> results = ComputeResults(CrashesAfterErrorMsg);

            Assert.AreEqual(3, results.Count);

            Assert.AreEqual("TestMath.AddFails", results[0].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[0].Outcome);
            Assert.IsFalse(results[0].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));

            Assert.AreEqual("TestMath.AddPasses", results[1].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Passed, results[1].Outcome);
            Assert.IsFalse(results[1].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));

            Assert.AreEqual("TestMath.Crash", results[2].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[2].Outcome);
            Assert.IsTrue(results[2].ErrorMessage.Contains(GoogleTestResultStandardOutputParser.CRASH_TEXT));
        }

        private List<TestResult> ComputeResults(List<string> consoleOutput)
        {
            List<TestCase> Cases = new List<TestCase>();
            Uri Uri = new Uri("http://nothing");
            Cases.Add(new TestCase("TestMath.AddFails", Uri, "SomeSource.cpp"));
            Cases.Add(new TestCase("TestMath.Crash", Uri, "SomeSource.cpp"));
            Cases.Add(new TestCase("TestMath.AddPasses", Uri, "SomeSource.cpp"));

            GoogleTestResultStandardOutputParser parser = new GoogleTestResultStandardOutputParser(consoleOutput, Cases);
            List<TestResult> results = parser.GetTestResults();
            return results;
        }

    }

}