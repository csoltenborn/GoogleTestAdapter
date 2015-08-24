using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Collections.Generic;

namespace GoogleTestAdapter
{

    public class GoogleTestCommandLine
    {

        private bool RunAll;
        private IEnumerable<TestCase> AllCases;
        private IEnumerable<TestCase> Cases;
        private string OutputPath;
        private IMessageLogger Logger;

        public GoogleTestCommandLine(bool runAll, IEnumerable<TestCase> allCases, IEnumerable<TestCase> cases, string outputPath, IMessageLogger logger)
        {
            this.RunAll = runAll;
            this.AllCases = allCases;
            this.Cases = cases;
            this.OutputPath = outputPath;
            this.Logger = logger;
        }

        public string GetCommandLine()
        {
            string CommandLine = string.Join(" ", GetOutputpathParameter(), GetFilterParameter());
            CommandLine += GetAlsoRunDisabledTestsParameter();
            CommandLine += GetShuffleTestsParameter();
            CommandLine += GetTestsRepetitionsParameter();
            return CommandLine;
        }

        private string GetOutputpathParameter()
        {
            return "--gtest_output=\"xml:" + OutputPath + "\"";
        }

        private string GetFilterParameter()
        {
            if (RunAll)
            {
                return "";
            }

            List<string> SuitesRunningAllTests = GetSuitesRunningAllTests();
            return "--gtest_filter=" + GetFilterForSuitesRunningAllTests(SuitesRunningAllTests) 
                + GetFilterForSuitesRunningIndividualTests(SuitesRunningAllTests);
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

        private string GetFilterForSuitesRunningIndividualTests(List<string> suitesRunningAllTests)
        {
            List<string> CasesNotHavingCommonSuite = new List<string>();
            foreach (TestCase testcase in Cases)
            {
                bool HasCommonSuite = false;
                foreach (string Suite in suitesRunningAllTests)
                {
                    if (Suite == TestsuiteNameFromCase(testcase))
                    {
                        HasCommonSuite = true;
                        break;
                    }
                }
                if (!HasCommonSuite)
                {
                    CasesNotHavingCommonSuite.Add(GetTestcaseNameForFiltering(testcase.FullyQualifiedName));
                }
            }
            return string.Join(":", CasesNotHavingCommonSuite);
        }

        private List<string> GetSuitesRunningAllTests()
        {
            List<string> SuitesRunningAllTests = new List<string>();
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

        private HashSet<string> GetDifferentSuites()
        {
            HashSet<string> Suites = new HashSet<string>();
            foreach (TestCase Testcase in Cases)
            {
                Suites.Add(TestsuiteNameFromCase(Testcase));
            }
            return Suites;
        }

        private List<TestCase> GetAllMatchingCases(IEnumerable<TestCase> cases, string suite)
        {
            List<TestCase> AllMatchingCases = new List<TestCase>();
            foreach(TestCase testcase in cases)
            {
                if (testcase.FullyQualifiedName.StartsWith(suite))
                {
                    AllMatchingCases.Add(testcase);
                }
            }
            return AllMatchingCases;
        }

        private string TestsuiteNameFromCase(TestCase testcase)
        {
            return testcase.FullyQualifiedName.Split(new char[] { '.' })[0];
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
