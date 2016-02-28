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
            public List<TestCase> TestCases { get; }
            public string CommandLine { get; }

            internal Args(List<TestCase> testCases, string commandLine)
            {
                this.TestCases = testCases ?? new List<TestCase>();
                this.CommandLine = commandLine ?? "";
            }
        }


        public const int MaxCommandLength = 8191;


        private int LengthOfExecutableString { get; }
        private IEnumerable<TestCase> AllTestCases { get; }
        private IEnumerable<TestCase> TestCasesToRun { get; }
        private string ResultXmlFile { get; }
        private TestEnvironment TestEnvironment { get; }
        private string UserParameters { get; }


        public CommandLineGenerator(
            IEnumerable<TestCase> allTestCases, IEnumerable<TestCase> testCasesToRun,
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
            baseCommandLine += GetCatchExceptionsParameter();
            baseCommandLine += GetBreakOnFailureParameter();
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

            List<TestCase> testCasesNotRunBySuite = GetTestCasesNotRunBySuite(suitesRunningAllTests);
            List<TestCase> testCasesRunBySuite = TestCasesToRun.Where(tc => !testCasesNotRunBySuite.Contains(tc)).ToList();
            if (testCasesNotRunBySuite.Count == 0)
            {
                commandLines.Add(new Args(TestCasesToRun.ToList(), baseCommandLineWithFilter + userParam));
                return commandLines;
            }

            List<TestCase> includedTestCases;
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

        private string JoinTestsUpToMaxLength(List<TestCase> testCases, int maxLength, out List<TestCase> includedTestCases)
        {
            includedTestCases = new List<TestCase>();
            if (testCases.Count == 0)
            {
                return "";
            }

            string result = "";
            string nextTest = testCases[0].FullyQualifiedName;
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
                    nextTest = ":" + testCases[0].FullyQualifiedName;
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

        private string GetCatchExceptionsParameter()
        {
            return GoogleTestConstants.GetCatchExceptionsOption(TestEnvironment.Options.CatchExceptions);
        }

        private string GetBreakOnFailureParameter()
        {
            return GoogleTestConstants.GetBreakOnFailureOption(TestEnvironment.Options.BreakOnFailure);
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
            HashSet<TestCase> allTestCasesAsSet = new HashSet<TestCase>(AllTestCases);
            HashSet<TestCase> testCasesToRunAsSet = new HashSet<TestCase>(TestCasesToRun);
            return allTestCasesAsSet.SetEquals(testCasesToRunAsSet);
        }

        private List<TestCase> GetTestCasesNotRunBySuite(List<string> suitesRunningAllTests)
        {
            List<TestCase> testCasesNotRunBySuite = new List<TestCase>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TestCase testCase in TestCasesToRun)
            {
                bool isRunBySuite = suitesRunningAllTests.Any(s => s == testCase.GetTestsuiteName_CommandLineGenerator());
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
                List<TestCase> allMatchingTestCasesToBeRun = GetAllMatchingTestCases(TestCasesToRun, suite);
                List<TestCase> allMatchingTestCases = GetAllMatchingTestCases(AllTestCases, suite);
                if (allMatchingTestCasesToBeRun.Count == allMatchingTestCases.Count)
                {
                    suitesRunningAllTests.Add(suite);
                }
            }
            return suitesRunningAllTests;
        }

        private List<string> GetAllSuitesOfTestCasesToRun()
        {
            return TestCasesToRun.Select(tc => tc.GetTestsuiteName_CommandLineGenerator()).Distinct().ToList();
        }

        private List<TestCase> GetAllMatchingTestCases(IEnumerable<TestCase> cases, string suite)
        {
            return cases.Where(testcase => suite == testcase.GetTestsuiteName_CommandLineGenerator()).ToList();
        }

    }

}