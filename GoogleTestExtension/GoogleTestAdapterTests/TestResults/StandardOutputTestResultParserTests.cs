using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class StandardOutputTestResultParserTests : AbstractGoogleTestExtensionTests
    {
        private string[] ConsoleOutput1 { get; } = {
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

        private string[] ConsoleOutput2 { get; } = {
            @"[       OK ] TestMath.AddPasses(0 ms)",
            @"[ RUN      ] TestMath.Crash",
            @"unknown file: error: SEH exception with code 0xc0000005 thrown in the test body.",
        };

        private string[] ConsoleOutput3 { get; } = {
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

        private List<string> CrashesImmediately { get; set; }
        private List<string> CrashesAfterErrorMsg { get; set; }
        private List<string> Complete { get; set; }

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            CrashesImmediately = new List<string>(ConsoleOutput1);

            CrashesAfterErrorMsg = new List<string>(ConsoleOutput1);
            CrashesAfterErrorMsg.AddRange(ConsoleOutput2);

            Complete = new List<string>(CrashesAfterErrorMsg);
            Complete.AddRange(ConsoleOutput3);
        }

        [TestMethod]
        public void TestCompleteOutput()
        {
            List<TestResult> results = ComputeResults(Complete);

            Assert.AreEqual(3, results.Count);

            Assert.AreEqual("TestMath.AddFails", results[0].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[0].Outcome);
            Assert.IsFalse(results[0].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(3), results[0].Duration);

            Assert.AreEqual("TestMath.AddPasses", results[1].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Passed, results[1].Outcome);
            Assert.IsFalse(results[1].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), results[1].Duration);

            Assert.AreEqual("TestMath.Crash", results[2].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[2].Outcome);
            Assert.IsFalse(results[2].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(9), results[2].Duration);
        }

        [TestMethod]
        public void TestOutputWithImmediateCrash()
        {
            List<TestResult> results = ComputeResults(CrashesImmediately);

            Assert.AreEqual(2, results.Count);

            Assert.AreEqual("TestMath.AddFails", results[0].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[0].Outcome);
            Assert.IsFalse(results[0].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(3), results[0].Duration);

            Assert.AreEqual("TestMath.AddPasses", results[1].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[1].Outcome);
            Assert.IsTrue(results[1].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(0), results[1].Duration);
        }

        [TestMethod]
        public void TestOutputWithCrashAfterErrorMessage()
        {
            List<TestResult> results = ComputeResults(CrashesAfterErrorMsg);

            Assert.AreEqual(3, results.Count);

            Assert.AreEqual("TestMath.AddFails", results[0].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[0].Outcome);
            Assert.IsFalse(results[0].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(3), results[0].Duration);

            Assert.AreEqual("TestMath.AddPasses", results[1].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Passed, results[1].Outcome);
            Assert.IsFalse(results[1].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(1), results[1].Duration);

            Assert.AreEqual("TestMath.Crash", results[2].TestCase.FullyQualifiedName);
            Assert.AreEqual(TestOutcome.Failed, results[2].Outcome);
            Assert.IsTrue(results[2].ErrorMessage.Contains(StandardOutputTestResultParser.CrashText));
            Assert.AreEqual(TimeSpan.FromMilliseconds(0), results[2].Duration);
        }

        private List<TestResult> ComputeResults(List<string> consoleOutput)
        {
            List<TestCase> cases = new List<TestCase>();
            Uri uri = new Uri("http://nothing");
            cases.Add(new TestCase("TestMath.AddFails", uri, "SomeSource.cpp"));
            cases.Add(new TestCase("TestMath.Crash", uri, "SomeSource.cpp"));
            cases.Add(new TestCase("TestMath.AddPasses", uri, "SomeSource.cpp"));

            StandardOutputTestResultParser parser = new StandardOutputTestResultParser(consoleOutput, cases, MockLogger.Object);
            List<TestResult> results = parser.GetTestResults();
            return results;
        }

    }

}