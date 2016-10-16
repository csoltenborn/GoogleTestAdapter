using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Tests.Common
{

    public class TestDataCreator
    {
        public const string DummyExecutable = "ff.exe";


        private readonly TestEnvironment _testEnvironment;

        public TestDataCreator(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
        }

        private List<TestCase> _allTestCasesExceptLoadTests;
        public List<TestCase> AllTestCasesExceptLoadTests
        {
            get
            {
                if (_allTestCasesExceptLoadTests == null)
                {
                    _allTestCasesExceptLoadTests = new List<TestCase>();
                    _allTestCasesExceptLoadTests.AddRange(AllTestCasesOfSampleTests);
                    _allTestCasesExceptLoadTests.AddRange(AllTestCasesOfHardCrashingTests);
                    _allTestCasesExceptLoadTests.AddRange(AllTestCasesOfLongRunningTests);
                }
                return _allTestCasesExceptLoadTests;
            }
        }

        private List<TestCase> _allTestCasesOfSampleTests;
        public List<TestCase> AllTestCasesOfSampleTests 
            => GetTestCases(TestResources.SampleTests, ref _allTestCasesOfSampleTests);

        private List<TestCase> _allTestCasesOfHardCrashindTests;
        public List<TestCase> AllTestCasesOfHardCrashingTests 
            => GetTestCases(TestResources.HardCrashingSampleTests, ref _allTestCasesOfHardCrashindTests);

        private List<TestCase> _allTestCasesOfLongRunningTests;
        public List<TestCase> AllTestCasesOfLongRunningTests 
            => GetTestCases(TestResources.LongRunningTests, ref _allTestCasesOfLongRunningTests);

        private List<TestCase> GetTestCases(string executable, ref List<TestCase> testCases)
        {
            if (testCases == null)
            {
                testCases = new List<TestCase>();
                var discoverer = new GoogleTestDiscoverer(_testEnvironment);
                testCases.AddRange(discoverer.GetTestsFromExecutable(executable));
            }
            return testCases;
        }

        public List<TestCase> GetTestCases(params string[] qualifiedNames)
        {
            return AllTestCasesExceptLoadTests.Where(
                testCase => qualifiedNames.Any(
                    qualifiedName => testCase.FullyQualifiedName.Contains(qualifiedName)))
                    .ToList();
        }

        public TestCase ToTestCase(string name, string executable, string sourceFile = "")
        {
            return new TestCase(name, executable, name, sourceFile, 0);
        }

        public TestCase ToTestCase(string name)
        {
            return ToTestCase(name, DummyExecutable);
        }

        public TestResult ToTestResult(string qualifiedTestCaseName, TestOutcome outcome, int duration, string executable = DummyExecutable)
        {
            return new TestResult(ToTestCase(qualifiedTestCaseName, executable))
            {
                Outcome = outcome,
                Duration = TimeSpan.FromMilliseconds(duration)
            };
        }

        public IEnumerable<TestCase> CreateDummyTestCases(params string[] qualifiedNames)
        {
            return qualifiedNames.Select(ToTestCase).ToList();
        }

    }

}