using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter
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
                }
                return _allTestCasesExceptLoadTests;
            }
        }

        private List<TestCase> _allTestCasesOfSampleTests;
        public List<TestCase> AllTestCasesOfSampleTests
        {
            get
            {
                if (_allTestCasesOfSampleTests == null)
                {
                    _allTestCasesOfSampleTests = new List<TestCase>();
                    GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(_testEnvironment);
                    _allTestCasesOfSampleTests.AddRange(discoverer.GetTestsFromExecutable(TestResources.SampleTests));
                }
                return _allTestCasesOfSampleTests;
            }
        }

        private List<TestCase> _allTestCasesOfHardCrashindTests;
        public List<TestCase> AllTestCasesOfHardCrashingTests
        {
            get
            {
                if (_allTestCasesOfHardCrashindTests == null)
                {
                    _allTestCasesOfHardCrashindTests = new List<TestCase>();
                    GoogleTestDiscoverer discoverer = new GoogleTestDiscoverer(_testEnvironment);
                    _allTestCasesOfHardCrashindTests.AddRange(discoverer.GetTestsFromExecutable(TestResources.HardCrashingSampleTests));
                }
                return _allTestCasesOfHardCrashindTests;
            }
        }

        public List<TestCase> GetTestCasesOfSampleTests(params string[] qualifiedNames)
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