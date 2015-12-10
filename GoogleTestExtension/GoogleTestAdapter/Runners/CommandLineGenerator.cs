using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Runners
{

    public class CommandLineGenerator
    {
        public class Args
        {
            public List<TestCase2> TestCases { get; }
            public string CommandLine { get; }

            internal Args(List<TestCase2> testCases, string commandLine)
            {
                this.TestCases = testCases ?? new List<TestCase2>();
                this.CommandLine = commandLine ?? "";
            }
        }


        public const int MaxCommandLength = 8191;


        private int LengthOfExecutableString { get; }
        private IEnumerable<TestCase2> AllTestCases { get; }
        private IEnumerable<TestCase2> TestCasesToRun { get; }
        private string ResultXmlFile { get; }
        private TestEnvironment TestEnvironment { get; }
        private string UserParameters { get; }


        public CommandLineGenerator(
            IEnumerable<TestCase2> allTestCases, IEnumerable<TestCase2> testCasesToRun,
            int lengthOfExecutableString, string userParameters, string resultXmlFile,
            TestEnvironment testEnvironment)
        {
            if (userParameters == null)
            {
                throw new ArgumentNullException(nameof(userParameters));
            }

            this.LengthOfExecutableString = lengthOfExecutableString;
            this.AllTestCases = allTestCases;
            this.TestCasesToRun = testCasesToRun;
            this.ResultXmlFile = resultXmlFile;
            this.TestEnvironment = testEnvironment;
            this.UserParameters = userParameters;
        }


        public IEnumerable<Args> GetCommandLines()
        {
            string baseCommandLine = GetOutputpathParameter();
            baseCommandLine += GetAlsoRunDisabledTestsParameter();
            baseCommandLine += GetShuffleTestsParameter();
            baseCommandLine += GetTestsRepetitionsParameter();

            List<Args> commandLines = new List<Args>();
            commandLines.AddRange(GetFinalCommandLines(baseCommandLine));
            return commandLines;
        }


        private IEnumerable<Args> GetFinalCommandLines(string baseCommandLine)
        {
            List<Args> commandLines = new List<Args>();
            string userParam = GetAdditionalUserParameter();
            if (AllTestCasesOfExecutableAreRun())
            {
                commandLines.Add(new Args(TestCasesToRun.ToList(), baseCommandLine + userParam));
                return commandLines;
            }

            List<string> suitesRunningAllTests = GetSuitesRunningAllTests();
            string suitesFilter = GoogleTestConstants.FilterOption
                + GetFilterForSuitesRunningAllTests(suitesRunningAllTests);
            string baseCommandLineWithFilter = baseCommandLine + suitesFilter;

            List<TestCase2> testCasesNotRunBySuite = GetTestCasesNotRunBySuite(suitesRunningAllTests);
            List<TestCase2> testCasesRunBySuite = TestCasesToRun.Where(tc => !testCasesNotRunBySuite.Contains(tc)).ToList();
            if (testCasesNotRunBySuite.Count == 0)
            {
                commandLines.Add(new Args(TestCasesToRun.ToList(), baseCommandLineWithFilter + userParam));
                return commandLines;
            }

            List<TestCase2> includedTestCases;
            int remainingLength = MaxCommandLength
                - baseCommandLineWithFilter.Length - LengthOfExecutableString - userParam.Length - 1;
            string commandLine = baseCommandLineWithFilter +
                JoinTestsUpToMaxLength(testCasesNotRunBySuite, remainingLength, out includedTestCases);
            includedTestCases.AddRange(testCasesRunBySuite);
            commandLines.Add(new Args(includedTestCases, commandLine + userParam));

            // only add suites to first command line
            baseCommandLineWithFilter = baseCommandLine + GoogleTestConstants.FilterOption;

            while (testCasesNotRunBySuite.Count > 0)
            {
                remainingLength = MaxCommandLength
                    - baseCommandLineWithFilter.Length - LengthOfExecutableString - userParam.Length - 1;
                commandLine = baseCommandLineWithFilter +
                              JoinTestsUpToMaxLength(testCasesNotRunBySuite, remainingLength, out includedTestCases);
                commandLines.Add(new Args(includedTestCases, commandLine + userParam));
            }

            return commandLines;
        }

        private string JoinTestsUpToMaxLength(List<TestCase2> testCases, int maxLength, out List<TestCase2> includedTestCases)
        {
            includedTestCases = new List<TestCase2>();
            if (testCases.Count == 0)
            {
                return "";
            }

            string result = "";
            string nextTest = GetTestcaseNameForFiltering(testCases[0].FullyQualifiedName);
            if (nextTest.Length > maxLength)
            {
                throw new Exception("CommandLineGenerator: I can not deal with this case :-( - maxLength=" + maxLength +
                    ", includedTestCases.Count=" + includedTestCases.Count + ", nextTest.Length=" + nextTest.Length);
            }

            while (result.Length + nextTest.Length <= maxLength && testCases.Count > 0)
            {
                result += nextTest;
                includedTestCases.Add(testCases[0]);
                testCases.RemoveAt(0);
                if (testCases.Count > 0)
                {
                    nextTest = ":" + GetTestcaseNameForFiltering(testCases[0].FullyQualifiedName);
                }
            }
            return result;
        }

        private string GetAdditionalUserParameter()
        {
            return UserParameters.Length == 0 ? "" : " " + UserParameters;
        }

        private string GetOutputpathParameter()
        {
            return GoogleTestConstants.GetResultXmlFileOption(ResultXmlFile);
        }

        private string GetAlsoRunDisabledTestsParameter()
        {
            return TestEnvironment.Options.RunDisabledTests
                ? GoogleTestConstants.AlsoRunDisabledTestsOption
                : "";
        }

        private string GetShuffleTestsParameter()
        {
            if (!TestEnvironment.Options.ShuffleTests)
            {
                return "";
            }

            string option = GoogleTestConstants.ShuffleTestsOption;

            int seed = TestEnvironment.Options.ShuffleTestsSeed;
            if (seed != GoogleTestConstants.ShuffleTestsSeedDefaultValue)
            {
                option += GoogleTestConstants.ShuffleTestsSeedOption + "=" + seed;
            }

            return option;
        }

        private string GetTestsRepetitionsParameter()
        {
            int nrOfRepetitions = TestEnvironment.Options.NrOfTestRepetitions;
            if (nrOfRepetitions == 1)
            {
                return "";
            }
            return GoogleTestConstants.NrOfRepetitionsOption + "=" + nrOfRepetitions;
        }

        private string GetFilterForSuitesRunningAllTests(List<string> suitesRunningAllTests)
        {
            return string.Join(".*:", suitesRunningAllTests).AppendIfNotEmpty(".*:");
        }

        private bool AllTestCasesOfExecutableAreRun()
        {
            HashSet<TestCase2> allTestCasesAsSet = new HashSet<TestCase2>(AllTestCases);
            HashSet<TestCase2> testCasesToRunAsSet = new HashSet<TestCase2>(TestCasesToRun);
            return allTestCasesAsSet.SetEquals(testCasesToRunAsSet);
        }

        private List<TestCase2> GetTestCasesNotRunBySuite(List<string> suitesRunningAllTests)
        {
            List<TestCase2> testCasesNotRunBySuite = new List<TestCase2>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TestCase2 testCase in TestCasesToRun)
            {
                bool isRunBySuite = suitesRunningAllTests.Any(s => s == GetTestsuiteName(testCase));
                if (!isRunBySuite)
                {
                    testCasesNotRunBySuite.Add(testCase);
                }
            }
            return testCasesNotRunBySuite;
        }

        private List<string> GetSuitesRunningAllTests()
        {
            List<string> suitesRunningAllTests = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string suite in GetAllSuitesOfTestCasesToRun())
            {
                List<TestCase2> allMatchingTestCasesToBeRun = GetAllMatchingTestCases(TestCasesToRun, suite);
                List<TestCase2> allMatchingTestCases = GetAllMatchingTestCases(AllTestCases, suite);
                if (allMatchingTestCasesToBeRun.Count == allMatchingTestCases.Count)
                {
                    suitesRunningAllTests.Add(suite);
                }
            }
            return suitesRunningAllTests;
        }

        private List<string> GetAllSuitesOfTestCasesToRun()
        {
            return TestCasesToRun.Select(GetTestsuiteName).Distinct().ToList();
        }

        private List<TestCase2> GetAllMatchingTestCases(IEnumerable<TestCase2> cases, string suite)
        {
            return cases.Where(testcase => suite == GetTestsuiteName(testcase)).ToList();
        }

        private string GetTestsuiteName(TestCase2 testCase)
        {
            return testCase.FullyQualifiedName.Split('.')[0];
        }

        private string GetTestcaseNameForFiltering(string fullname)
        {
            int index = fullname.IndexOf(GoogleTestConstants.ParameterizedTestMarker,
                StringComparison.Ordinal);
            if (index < 0)
            {
                return fullname;
            }
            return fullname.Substring(0, index);
        }

    }

}