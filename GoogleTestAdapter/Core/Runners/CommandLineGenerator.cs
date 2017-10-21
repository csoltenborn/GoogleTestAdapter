// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;

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

        private readonly int _lengthOfExecutableString;
        private readonly IList<TestCase> _testCasesToRun;
        private readonly string _resultXmlFile;
        private readonly SettingsWrapper _settings;
        private readonly string _userParameters;

        public CommandLineGenerator(IEnumerable<TestCase> testCasesToRun,
            int lengthOfExecutableString, string userParameters, string resultXmlFile,
            SettingsWrapper settings)
        {
            if (userParameters == null)
                throw new ArgumentNullException(nameof(userParameters));

            _lengthOfExecutableString = lengthOfExecutableString;
            _testCasesToRun = testCasesToRun.ToList();
            _resultXmlFile = resultXmlFile;
            _settings = settings;
            _userParameters = userParameters;
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
            var commandLines = new List<Args>();
            string userParam = GetAdditionalUserParameter();
            if (AllTestCasesOfExecutableAreRun())
            {
                commandLines.Add(new Args(_testCasesToRun, baseCommandLine + userParam));
                return commandLines;
            }

            string baseCommandLineWithFilter = baseCommandLine + GoogleTestConstants.FilterOption;

            List<string> suitesRunningAllTests = GetSuitesRunningAllTests();
            baseCommandLineWithFilter += GetFilterForSuitesRunningAllTests(suitesRunningAllTests);

            List<TestCase> testCasesNotRunBySuite = GetTestCasesNotRunBySuite(suitesRunningAllTests);
            List<TestCase> testCasesRunBySuite = _testCasesToRun.Where(tc => !testCasesNotRunBySuite.Contains(tc)).ToList();
            if (testCasesNotRunBySuite.Count == 0)
            {
                commandLines.Add(new Args(_testCasesToRun, baseCommandLineWithFilter + userParam));
                return commandLines;
            }

            List<TestCase> includedTestCases;
            int remainingLength = MaxCommandLength
                - baseCommandLineWithFilter.Length - _lengthOfExecutableString - userParam.Length - 1;
            string commandLine = baseCommandLineWithFilter +
                JoinTestsUpToMaxLength(testCasesNotRunBySuite, remainingLength, out includedTestCases);
            includedTestCases.AddRange(testCasesRunBySuite);
            commandLines.Add(new Args(includedTestCases, commandLine + userParam));

            // only add suites to first command line
            baseCommandLineWithFilter = baseCommandLine + GoogleTestConstants.FilterOption;

            while (testCasesNotRunBySuite.Count > 0)
            {
                remainingLength = MaxCommandLength
                    - baseCommandLineWithFilter.Length - _lengthOfExecutableString - userParam.Length - 1;
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
                throw new Exception(String.Format(Resources.CommandLineGeneratorError, maxLength, includedTestCases.Count, nextTest.Length));
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
            return string.Join(".*:", suitesRunningAllTests).AppendIfNotEmpty(".*:");
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
            var testCasesNotRunBySuite = new List<TestCase>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TestCase testCase in _testCasesToRun)
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