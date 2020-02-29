using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Runners
{

    public class CommandLineGenerator
    {
        public class Args
        {
            public IList<TestCase> TestCases { get; }
            public string CommandLine { get; }

            internal Args(IList<TestCase> testCases, string commandLine)
            {
                TestCases = testCases ?? new List<TestCase>();
                CommandLine = commandLine ?? "";
            }
        }

        // TODO change to new MaxCommandLength asa only ProcessExecutor is used (32768, see https://msdn.microsoft.com/en-us/library/windows/desktop/ms682425%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396)
        public const int MaxCommandLength = 8191;
        private const string _suiteDelimiter = ".*:";
        private readonly int _lengthOfExecutableString;
        private readonly IList<TestCase> _testCasesToRun;
        private readonly string _resultXmlFile;
        private readonly SettingsWrapper _settings;
        private readonly string _userParameters;

        public CommandLineGenerator(IEnumerable<TestCase> testCasesToRun,
            int lengthOfExecutableString, string userParameters, string resultXmlFile,
            SettingsWrapper settings)
        {
            _lengthOfExecutableString = lengthOfExecutableString;
            _testCasesToRun = testCasesToRun.ToList();
            _resultXmlFile = resultXmlFile;
            _settings = settings;
            _userParameters = userParameters ?? throw new ArgumentNullException(nameof(userParameters));
        }

        public IEnumerable<Args> GetCommandLines()
        {
            string baseCommandLine = GetOutputpathParameter();
            baseCommandLine += GetCatchExceptionsParameter();
            baseCommandLine += GetBreakOnFailureParameter();
            baseCommandLine += GetAlsoRunDisabledTestsParameter();
            baseCommandLine += GetShuffleTestsParameter();
            baseCommandLine += GetTestsRepetitionsParameter();

            var commandLines = new List<Args>();
            commandLines.AddRange(GetFinalCommandLines(baseCommandLine));
            return commandLines;
        }

        private IEnumerable<Args> GetFinalCommandLines(string baseCommandLine)
        {
            var exitCodeTest =
                _testCasesToRun.SingleOrDefault(tc => tc.IsExitCodeTestCase);
            if (exitCodeTest != null)
            {
                _testCasesToRun.Remove(exitCodeTest);
                if (!_testCasesToRun.Any())
                {
                    return CreateDummyCommandLineArgs(baseCommandLine, exitCodeTest).Yield();
                }
            }

            var commandLines = new List<Args>();
            string userParam = GetAdditionalUserParameter();
            if (AllTestCasesOfExecutableAreRun())
            {
                commandLines.Add(new Args(_testCasesToRun, baseCommandLine + userParam));
                return commandLines;
            }

            List<string> suitesRunningAllTests = GetSuitesRunningAllTests();
            int maxSuiteLength = MaxCommandLength - _lengthOfExecutableString - userParam.Length - 1;

            List<List<string>> suiteLists = GetSuiteListsForCommandLines(suitesRunningAllTests, maxSuiteLength);
            
            // lambda to return the base commandline string (including suite filters) and the list of testcases to execute
            var getFilterAndTestCasesForSuites = Functional.ToFunc((List<string> suites) =>
            {
                string suiteNamesFilter = GetFilterForSuitesRunningAllTests(suites);
                System.Diagnostics.Debug.Assert(suiteNamesFilter.Length < maxSuiteLength);

                string baseCommandLineWithFilter = baseCommandLine + GoogleTestConstants.FilterOption + suiteNamesFilter;
                List<TestCase> testCases = suites.Select(GetTestCasesRunBySuite).SelectMany(testCase => testCase).ToList();
                return new { baseCommandLineWithFilter, testCases };
            });

            // process all but the last suites-list
            suiteLists.Take(suiteLists.Count - 1)
                      .Select(getFilterAndTestCasesForSuites).ToList()
                      .ForEach(suitesFilterAndTests
                               => commandLines.Add(new Args(suitesFilterAndTests.testCases, suitesFilterAndTests.baseCommandLineWithFilter + userParam)));

            // process the last suite-list and all test-cases which belong to test-suites that are not fully executed
            var filterAndTestsForLastSuites = getFilterAndTestCasesForSuites(suiteLists.Last());
            string baseCommandLineForLastSuites = filterAndTestsForLastSuites.baseCommandLineWithFilter;

            List<TestCase> testCasesNotRunBySuite = GetTestCasesNotRunBySuite(suitesRunningAllTests);

            int remainingLength = MaxCommandLength - baseCommandLineForLastSuites.Length - _lengthOfExecutableString - userParam.Length - 1;
            string commandLine = baseCommandLineForLastSuites + JoinTestsUpToMaxLength(testCasesNotRunBySuite, remainingLength, out var includedTestCases);
            includedTestCases.AddRange(filterAndTestsForLastSuites.testCases);

            // a single command line holding both suite- and single-tests-filters
            commandLines.Add(new Args(includedTestCases, commandLine + userParam));

            // command lines holding only single-test- but no suite-filters
            string baseCommandLineWithoutSuites = baseCommandLine + GoogleTestConstants.FilterOption;
            while (testCasesNotRunBySuite.Count > 0)
            {
                remainingLength = MaxCommandLength - baseCommandLineWithoutSuites.Length - _lengthOfExecutableString - userParam.Length - 1;
                commandLine = baseCommandLineWithoutSuites + JoinTestsUpToMaxLength(testCasesNotRunBySuite, remainingLength, out includedTestCases);
                commandLines.Add(new Args(includedTestCases, commandLine + userParam));
            }

            return commandLines;
        }

        private Args CreateDummyCommandLineArgs(string baseCommandLine, TestCase exitCodeTest)
        {
            string commandLine = baseCommandLine;
            commandLine += $"{GoogleTestConstants.FilterOption}GTA_NOT_EXISTING_DUMMY_TEST_CASE";
            commandLine += GetAdditionalUserParameter();

            return new Args(new List<TestCase> { exitCodeTest }, commandLine);
        }

        /// <summary>
        /// Returns a list of test-suite-name-lists, each of which shall fit as filter on a single command-line.
        /// </summary>
        /// <param name="suitesRunningAllTests">list of test-suites, each of which is to be executed fully (i.e. all test-cases of the suite are executed)</param>
        /// <param name="maxSuiteLength">the maximum length of all suite-filters allowed for a single command line</param>
        /// <returns>List of test-suite-name-lists, each to which to be executed by a single command-line</returns>
        private static List<List<string>> GetSuiteListsForCommandLines(List<string> suitesRunningAllTests, int maxSuiteLength)
        {
            int suiteLengthAggregate = 0;
            int groupIdxCounter = 0;
            int suiteDelimLen = _suiteDelimiter.Length;

            var suiteGroups = suitesRunningAllTests
                    .Select(suite =>
                    {
                        suiteLengthAggregate += suite.Length + suiteDelimLen;
                        if (suiteLengthAggregate > maxSuiteLength)
                        {
                            ++groupIdxCounter;
                            suiteLengthAggregate = suite.Length + suiteDelimLen;
                        }
                        return new { groupIdx = groupIdxCounter, suiteName = suite };
                    })
                    .GroupBy(pair => pair.groupIdx, pair => pair.suiteName)
                    .ToList();

            var suiteLists = suiteGroups.Select(group => group.ToList()).ToList();
            if (!suiteLists.Any()) suiteLists.Add(new List<string>());
            return suiteLists;
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
            return _userParameters.Length == 0 ? "" : " " + _userParameters;
        }

        private string GetOutputpathParameter()
        {
            return GoogleTestConstants.GetResultXmlFileOption(_resultXmlFile);
        }

        private string GetCatchExceptionsParameter()
        {
            return GoogleTestConstants.GetCatchExceptionsOption(_settings.CatchExceptions);
        }

        private string GetBreakOnFailureParameter()
        {
            return GoogleTestConstants.GetBreakOnFailureOption(_settings.BreakOnFailure);
        }

        private string GetAlsoRunDisabledTestsParameter()
        {
            return _settings.RunDisabledTests
                ? GoogleTestConstants.AlsoRunDisabledTestsOption
                : "";
        }

        private string GetShuffleTestsParameter()
        {
            if (!_settings.ShuffleTests)
            {
                return "";
            }

            string option = GoogleTestConstants.ShuffleTestsOption;

            int seed = _settings.ShuffleTestsSeed;
            if (seed != GoogleTestConstants.ShuffleTestsSeedDefaultValue)
            {
                option += GoogleTestConstants.ShuffleTestsSeedOption + "=" + seed;
            }

            return option;
        }

        private string GetTestsRepetitionsParameter()
        {
            int nrOfRepetitions = _settings.NrOfTestRepetitions;
            if (nrOfRepetitions == 1)
            {
                return "";
            }
            return GoogleTestConstants.NrOfRepetitionsOption + "=" + nrOfRepetitions;
        }

        private string GetFilterForSuitesRunningAllTests(List<string> suitesRunningAllTests)
        {
            return string.Join(_suiteDelimiter, suitesRunningAllTests).AppendIfNotEmpty(_suiteDelimiter);
        }

        private bool AllTestCasesOfExecutableAreRun()
        {
            if (!_testCasesToRun.Any())
                return true;

            TestCaseMetaDataProperty metaData = _testCasesToRun.First().Properties
                .OfType<TestCaseMetaDataProperty>()
                .SingleOrDefault();
            if (metaData == null)
                throw new Exception($"Test does not have meta data: {_testCasesToRun.First()}");

            return _testCasesToRun.Count == metaData.NrOfTestCasesInExecutable;
        }

        private List<TestCase> GetTestCasesNotRunBySuite(List<string> suitesRunningAllTests)
        {
            return _testCasesToRun.Where(t => !suitesRunningAllTests.Contains(GetTestsuiteName(t))).ToList();
        }

        private List<TestCase> GetTestCasesRunBySuite(string suite)
        {
            return _testCasesToRun.Where(t => GetTestsuiteName(t) == suite).ToList();
        }


        private List<string> GetSuitesRunningAllTests()
        {
            var suitesRunningAllTests = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string suite in GetAllSuitesOfTestCasesToRun())
            {
                List<TestCase> allMatchingTestCasesToBeRun = GetAllMatchingTestCases(_testCasesToRun, suite);
                TestCaseMetaDataProperty metaData = allMatchingTestCasesToBeRun.First().Properties
                    .OfType<TestCaseMetaDataProperty>()
                    .SingleOrDefault();
                if (metaData == null)
                    throw new Exception($"Test does not have meta data: {allMatchingTestCasesToBeRun.First()}");

                if (allMatchingTestCasesToBeRun.Count == metaData.NrOfTestCasesInSuite)
                    suitesRunningAllTests.Add(suite);
            }
            return suitesRunningAllTests;
        }

        private List<string> GetAllSuitesOfTestCasesToRun()
        {
            return _testCasesToRun.Select(GetTestsuiteName).Distinct().ToList();
        }

        private List<TestCase> GetAllMatchingTestCases(IEnumerable<TestCase> cases, string suite)
        {
            return cases.Where(testcase => suite == GetTestsuiteName(testcase)).ToList();
        }

        private string GetTestsuiteName(TestCase testCase)
        {
            return testCase.FullyQualifiedName.Split('.')[0];
        }

    }
}