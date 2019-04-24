using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Tests.Common.Helpers;

namespace GoogleTestAdapter.Tests.Common
{

    public class TestDataCreator
    {
        public const string DummyExecutable = "c:\\ff.exe";


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
            => GetTestCases(TestResources.Tests_DebugX86, ref _allTestCasesOfSampleTests);

        private List<TestCase> _allTestCasesOfHardCrashindTests;
        public List<TestCase> AllTestCasesOfHardCrashingTests 
            => GetTestCases(TestResources.CrashingTests_DebugX86, ref _allTestCasesOfHardCrashindTests);

        private List<TestCase> _allTestCasesOfLongRunningTests;
        public List<TestCase> AllTestCasesOfLongRunningTests 
            => GetTestCases(TestResources.LongRunningTests_ReleaseX86, ref _allTestCasesOfLongRunningTests);

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
                if (!suite2TestCases.TryGetValue(suite, out var testCasesWithSuiteName))
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

        public static string PreparePathExtensionTest()
        {
            string baseDir = Utils.GetTempDirectory();
            string exeDir = Path.Combine(baseDir, "exe");
            string dllDir = Path.Combine(baseDir, "dll");
            string targetExe = GetPathExtensionExecutable(baseDir);
            string targetDll = Path.Combine(dllDir, Path.GetFileName(TestResources.DllTestsDll_ReleaseX86));

            Directory.CreateDirectory(exeDir);
            Directory.CreateDirectory(dllDir);
            File.Copy(TestResources.DllTests_ReleaseX86, targetExe);
            File.Copy(TestResources.DllTestsDll_ReleaseX86, targetDll);

            return baseDir;
        }

        public static string GetPathExtensionExecutable(string baseDir)
        {
            return Path.Combine(baseDir, "exe", Path.GetFileName(TestResources.DllTests_ReleaseX86));
        }
    }

}