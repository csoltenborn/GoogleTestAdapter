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
        public class Args
        {
            public List<TestCase> TestCases { get; }
            public string CommandLine { get; }

            public Args(List<TestCase> testCases, string commandLine)
            {
                this.TestCases = testCases ?? new List<TestCase>();
                this.CommandLine = commandLine ?? "";
            }
        }


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

        public IEnumerable<Args> GetCommandLines()
        {
            string BaseCommandLine = GetOutputpathParameter();
            BaseCommandLine += GetAlsoRunDisabledTestsParameter();
            BaseCommandLine += GetShuffleTestsParameter();
            BaseCommandLine += GetTestsRepetitionsParameter();

            List<Args> CommandLines = new List<Args>();
            CommandLines.AddRange(GetFinalCommandLines(BaseCommandLine));
            return CommandLines;
        }

        private IEnumerable<Args> GetFinalCommandLines(string baseCommandLine)
        {
            List<Args> CommandLines = new List<Args>();
            if (RunAllTestCases)
            {
                CommandLines.Add(new Args(CasesToRun.ToList(), baseCommandLine));
                return CommandLines;
            }

            List<string> SuitesRunningAllTests = GetSuitesRunningAllTests();
            string BaseFilter = " --gtest_filter=" + GetFilterForSuitesRunningAllTests(SuitesRunningAllTests);
            string BaseCommandLine = baseCommandLine + BaseFilter;

            List<TestCase> TestsNotRunBySuite = GetCasesNotRunBySuite(SuitesRunningAllTests);
            List<TestCase> TestsRunBySuite = CasesToRun.Where(TC => !TestsNotRunBySuite.Contains(TC)).ToList();
            if (TestsNotRunBySuite.Count == 0)
            {
                CommandLines.Add(new Args(CasesToRun.ToList(), BaseCommandLine));
                return CommandLines;
            }

            List<TestCase> IncludedTestCases;
            string CommandLine = BaseCommandLine +
                                 JoinTestsUpToMaxLength(TestsNotRunBySuite,
                                     MAX_COMMAND_LENGTH - BaseCommandLine.Length - LengthOfExecutableString - 1,
                                     out IncludedTestCases);
            IncludedTestCases.AddRange(TestsRunBySuite);
            CommandLines.Add(new Args(IncludedTestCases, CommandLine));
            BaseCommandLine = baseCommandLine + " --gtest_filter="; // only add suites to first command line

            while (TestsNotRunBySuite.Count > 0)
            {
                CommandLine = BaseCommandLine +
                              JoinTestsUpToMaxLength(TestsNotRunBySuite,
                                  MAX_COMMAND_LENGTH - BaseCommandLine.Length - LengthOfExecutableString - 1,
                                  out IncludedTestCases);
                CommandLines.Add(new Args(IncludedTestCases, CommandLine));
            }

            return CommandLines;
        }

        private string JoinTestsUpToMaxLength(List<TestCase> tests, int maxLength, out List<TestCase> includedTestCases)
        {
            includedTestCases = new List<TestCase>();
            if (tests.Count == 0)
            {
                return "";
            }

            string Result = "";
            string NextTest = GetTestcaseNameForFiltering(tests[0].FullyQualifiedName);
            if (NextTest.Length > maxLength)
            {
                throw new Exception("I can not deal with this case :-(");
            }

            while (Result.Length + NextTest.Length <= maxLength && tests.Count > 0)
            {
                Result += NextTest;
                includedTestCases.Add(tests[0]);
                tests.RemoveAt(0);
                if (tests.Count > 0)
                {
                    NextTest = ":" + GetTestcaseNameForFiltering(tests[0].FullyQualifiedName);
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

        private List<TestCase> GetCasesNotRunBySuite(List<string> suitesRunningAllTests)
        {
            List<TestCase> CasesNotRunBySuite = new List<TestCase>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TestCase TestCase in CasesToRun)
            {
                bool IsRunBySuite = suitesRunningAllTests.Any(Suite => Suite == GetTestsuiteNameFromCase(TestCase));
                if (!IsRunBySuite)
                {
                    CasesNotRunBySuite.Add(TestCase);
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
