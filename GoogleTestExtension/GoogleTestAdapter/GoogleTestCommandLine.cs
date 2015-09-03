using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;

namespace GoogleTestAdapter
{

    public class GoogleTestCommandLine
    {
        public const int MAX_COMMAND_LENGTH = 8191;

        private readonly bool RunAllTestCases;
        private readonly int LengthOfExecutableString;
        private readonly IEnumerable<TestCase> AllCases;
        private readonly IEnumerable<TestCase> CasesToRun;
        private readonly string ResultXmlFile;
        private readonly IMessageLogger Logger;
        private readonly IOptions Options;

        public GoogleTestCommandLine(bool runAllTestCases, int lengthOfExecutableString, IEnumerable<TestCase> allCases, IEnumerable<TestCase> casesToRun, string resultXmlFile, IMessageLogger logger, IOptions options)
        {
            this.RunAllTestCases = runAllTestCases;
            this.LengthOfExecutableString = lengthOfExecutableString;
            this.AllCases = allCases;
            this.CasesToRun = casesToRun;
            this.ResultXmlFile = resultXmlFile;
            this.Logger = logger;
            this.Options = options;
        }

        public IEnumerable<string> GetCommandLines()
        {
            string BaseCommandLine = GetOutputpathParameter();
            BaseCommandLine += GetAlsoRunDisabledTestsParameter();
            BaseCommandLine += GetShuffleTestsParameter();
            BaseCommandLine += GetTestsRepetitionsParameter();

            List<string> CommandLines = new List<string>();
            CommandLines.AddRange(GetFinalCommandLines(BaseCommandLine));
            return CommandLines;
        }

        private IEnumerable<string> GetFinalCommandLines(string baseCommandLine)
        {
            List<string> CommandLines = new List<string>();
            if (RunAllTestCases)
            {
                CommandLines.Add(baseCommandLine);
                return CommandLines;
            }

            List<string> SuitesRunningAllTests = GetSuitesRunningAllTests();
            string BaseFilter = " --gtest_filter=" + GetFilterForSuitesRunningAllTests(SuitesRunningAllTests);
            string BaseCommandLine = baseCommandLine + BaseFilter;

            List<string> TestsNotRunBySuite = GetCasesNotRunBySuite(SuitesRunningAllTests);
            if (TestsNotRunBySuite.Count == 0)
            {
                CommandLines.Add(BaseCommandLine);
                return CommandLines;
            }

            CommandLines.Add(BaseCommandLine + JoinTestsUpToMaxLength(TestsNotRunBySuite, MAX_COMMAND_LENGTH - BaseCommandLine.Length - LengthOfExecutableString - 1));
            BaseCommandLine = baseCommandLine + " --gtest_filter="; // only add suites to first command line

            while (TestsNotRunBySuite.Count > 0)
            {
                CommandLines.Add(BaseCommandLine + JoinTestsUpToMaxLength(TestsNotRunBySuite, MAX_COMMAND_LENGTH - BaseCommandLine.Length - LengthOfExecutableString - 1));
            }

            return CommandLines;
        }

        private string JoinTestsUpToMaxLength(List<string> tests, int maxLength)
        {
            if (tests.Count == 0)
            {
                return "";
            }

            string Result = "";
            string NextTest = tests[0];
            if (NextTest.Length > maxLength)
            {
                throw new Exception("I can not deal with this case :-(");
            }

            while (Result.Length + NextTest.Length <= maxLength && tests.Count > 0)
            {
                Result += NextTest;
                tests.RemoveAt(0);
                if (tests.Count > 0)
                {
                    NextTest = ":" + tests[0];
                }
            }
            return Result;
        } 

        private string GetOutputpathParameter()
        {
            return "--gtest_output=\"xml:" + ResultXmlFile + "\"";
        }

        private string GetAlsoRunDisabledTestsParameter()
        {
            return Options.RunDisabledTests ? " --gtest_also_run_disabled_tests" : "";
        }

        private string GetShuffleTestsParameter()
        {
            return Options.ShuffleTests ? " --gtest_shuffle" : "";
        }

        private string GetTestsRepetitionsParameter()
        {
            int NrOfRepetitions = Options.NrOfTestRepetitions;
            if (NrOfRepetitions == 1)
            {
                return "";
            }
            if (NrOfRepetitions == 0 || NrOfRepetitions < -1)
            {
                Logger.SendMessage(TestMessageLevel.Error,
                    "Test level repetitions configured under Options/Google Test Adapter is " +
                    NrOfRepetitions + ", should be -1 (infinite) or > 0. Ignoring value.");
                return "";
            }
            return " --gtest_repeat=" + NrOfRepetitions;
        }

        private string GetFilterForSuitesRunningAllTests(List<string> suitesRunningAllTests)
        {
            return string.Join(".*:", suitesRunningAllTests).AppendIfNotEmpty(".*:");
        }

        private List<string> GetCasesNotRunBySuite(List<string> suitesRunningAllTests)
        {
            List<string> CasesNotRunBySuite = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TestCase TestCase in CasesToRun)
            {
                bool IsRunBySuite = suitesRunningAllTests.Any(Suite => Suite == GetTestsuiteNameFromCase(TestCase));
                if (!IsRunBySuite)
                {
                    CasesNotRunBySuite.Add(GetTestcaseNameForFiltering(TestCase.FullyQualifiedName));
                }
            }
            return CasesNotRunBySuite;
        }

        private List<string> GetSuitesRunningAllTests()
        {
            List<string> SuitesRunningAllTests = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string Suite in GetAllSuitesOfTestCasesToRun())
            {
                List<TestCase> AllMatchingCasesToBeRun = GetAllMatchingCases(CasesToRun, Suite);
                List<TestCase> AllMatchingCases = GetAllMatchingCases(AllCases, Suite);
                if (AllMatchingCasesToBeRun.Count == AllMatchingCases.Count)
                {
                    SuitesRunningAllTests.Add(Suite);
                }
            }
            return SuitesRunningAllTests;
        }

        private List<string> GetAllSuitesOfTestCasesToRun()
        {
            // TODO remove debug code
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            List<string> Result = CasesToRun.Select(GetTestsuiteNameFromCase).Distinct().ToList();
            Stopwatch.Stop();
            Logger.SendMessage(TestMessageLevel.Informational, "Duration for GetAllSuitesOfTestCasesToRun(): " + Stopwatch.Elapsed);
            return Result;
        }

        private List<TestCase> GetAllMatchingCases(IEnumerable<TestCase> cases, string suite)
        {
            return cases.Where(testcase => suite == GetTestsuiteNameFromCase(testcase)).ToList();
        }

        private string GetTestsuiteNameFromCase(TestCase testcase)
        {
            return testcase.FullyQualifiedName.Split('.')[0];
        }

        private string GetTestcaseNameForFiltering(string fullname)
        {
            int Index = fullname.IndexOf(' ');
            if (Index < 0)
            {
                return fullname;
            }
            return fullname.Substring(0, Index);
        }

    }

}
