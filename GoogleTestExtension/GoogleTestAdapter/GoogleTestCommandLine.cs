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
        private static readonly char[] SPLIT_SUITE = new char[] { '.' };

        private readonly bool RunAll;
        private readonly int LengthOfExecutable;
        private readonly IEnumerable<TestCase> AllCases;
        private readonly IEnumerable<TestCase> Cases;
        private readonly string OutputPath;
        private readonly IMessageLogger Logger;
        private readonly IOptions Options;

        public GoogleTestCommandLine(bool runAll, int lengthOfExecutable, IEnumerable<TestCase> allCases, IEnumerable<TestCase> cases, string outputPath, IMessageLogger logger, IOptions options)
        {
            this.RunAll = runAll;
            this.LengthOfExecutable = lengthOfExecutable;
            this.AllCases = allCases;
            this.Cases = cases;
            this.OutputPath = outputPath;
            this.Logger = logger;
            this.Options = options;
        }

        public IEnumerable<string> GetCommandLines()
        {
            string BaseCommand = GetOutputpathParameter();
            BaseCommand += GetAlsoRunDisabledTestsParameter();
            BaseCommand += GetShuffleTestsParameter();
            BaseCommand += GetTestsRepetitionsParameter();

            List<string> Commands = new List<string>();
            Commands.AddRange(GetFinalCommands(BaseCommand));
            return Commands;
        }

        private IEnumerable<string> GetFinalCommands(string baseCommand)
        {
            List<string> Commands = new List<string>();
            if (RunAll)
            {
                Commands.Add(baseCommand + " ");
                return Commands;
            }

            List<string> SuitesRunningAllTests = GetSuitesRunningAllTests();
            string BaseFilter = " --gtest_filter=" + GetFilterForSuitesRunningAllTests(SuitesRunningAllTests);
            string BaseCommand = baseCommand + BaseFilter;

            List<string> TestsWithoutCommonSuite = GetCasesNotHavingCommonSuite(SuitesRunningAllTests);
            if (TestsWithoutCommonSuite.Count == 0)
            {
                Commands.Add(BaseCommand);
                return Commands;
            }
            while (TestsWithoutCommonSuite.Count > 0)
            {
                Commands.Add(BaseCommand + GetJoinOfMaxLength(TestsWithoutCommonSuite, MAX_COMMAND_LENGTH - BaseCommand.Length - LengthOfExecutable - 1));
                BaseCommand = baseCommand + " --gtest_filter=";
            }

            return Commands;
        }

        private string GetJoinOfMaxLength(List<string> tests, int maxLength)
        {
            if (tests.Count == 0)
            {
                return "";
            }

            string Result = "";
            string NextTest = tests[0];
            if (NextTest.Length > maxLength)
            {
                throw new Exception();
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
            return "--gtest_output=\"xml:" + OutputPath + "\"";
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

        private List<string> GetCasesNotHavingCommonSuite(List<string> suitesRunningAllTests)
        {
            List<string> CasesNotHavingCommonSuite = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TestCase testcase in Cases)
            {
                bool HasCommonSuite = suitesRunningAllTests.Any(Suite => Suite == TestsuiteNameFromCase(testcase));
                if (!HasCommonSuite)
                {
                    CasesNotHavingCommonSuite.Add(GetTestcaseNameForFiltering(testcase.FullyQualifiedName));
                }
            }
            return CasesNotHavingCommonSuite;
        }

        private List<string> GetSuitesRunningAllTests()
        {
            List<string> SuitesRunningAllTests = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string Suite in GetDifferentSuites())
            {
                List<TestCase> AllMatchingCasesToBeRun = GetAllMatchingCases(Cases, Suite);
                List<TestCase> AllMatchingCases = GetAllMatchingCases(AllCases, Suite);
                if (AllMatchingCasesToBeRun.Count == AllMatchingCases.Count)
                {
                    SuitesRunningAllTests.Add(Suite);
                }
            }
            return SuitesRunningAllTests;
        }

        private List<string> GetDifferentSuites()
        {
            // TODO remove debug code
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            List<string> Result = Cases.AsParallel().Select(TestsuiteNameFromCase).Distinct().ToList();
            Stopwatch.Stop();
            Logger.SendMessage(TestMessageLevel.Informational, "Duration fo GetDifferentSuites(): " + Stopwatch.Elapsed);
            return Result;
        }

        private List<TestCase> GetAllMatchingCases(IEnumerable<TestCase> cases, string suite)
        {
            return cases.Where(testcase => suite == TestsuiteNameFromCase(testcase)).ToList();
        }

        private string TestsuiteNameFromCase(TestCase testcase)
        {
            return testcase.FullyQualifiedName.Split(SPLIT_SUITE)[0];
        }

        private string GetTestcaseNameForFiltering(string fullname)
        {
            int index = fullname.IndexOf(' ');
            if (index < 0)
            {
                return fullname;
            }
            return fullname.Substring(0, index);
        }

    }

}
