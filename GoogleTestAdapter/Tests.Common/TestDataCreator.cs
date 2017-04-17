using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Tests.Common.Helpers;

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
                var discoverer = new GoogleTestDiscoverer(_testEnvironment.Logger, _testEnvironment.Options);
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

        public IEnumerable<TestCase> CreateDummyTestCasesFull(string[] qualifiedNamesToRun, string[] allQualifiedNames)
        {
            IDictionary<string, ISet<TestCase>> suite2TestCases = new Dictionary<string, ISet<TestCase>>();
            foreach (string qualifiedName in allQualifiedNames)
            {
                TestCase testCase = ToTestCase(qualifiedName);

                int index = qualifiedName.LastIndexOf(".", StringComparison.Ordinal);
                string suite = qualifiedName.Substring(0, index);
                ISet<TestCase> testCasesWithSuiteName;
                if (!suite2TestCases.TryGetValue(suite, out testCasesWithSuiteName))
                    suite2TestCases.Add(suite, testCasesWithSuiteName = new HashSet<TestCase>());
                testCasesWithSuiteName.Add(testCase);
            }

            var testCases = new List<TestCase>();
            foreach (var suiteTestCasePair in suite2TestCases)
            {
                foreach (var testCase in suiteTestCasePair.Value)
                {
                    if (qualifiedNamesToRun.Contains(testCase.FullyQualifiedName))
                    {
                        testCase.Properties.Add(new TestCaseMetaDataProperty(suiteTestCasePair.Value.Count, allQualifiedNames.Length));
                        testCases.Add(testCase);
                    }
                }
            }

            return testCases;
        }

        public IEnumerable<TestCase> CreateDummyTestCases(params string[] qualifiedNames)
        {
            return CreateDummyTestCasesFull(qualifiedNames, qualifiedNames);
        }

    }

}