using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;

namespace GoogleTestAdapter
{

    public class GoogleTestCommandLine
    {

        private bool RunAll;
        private IEnumerable<TestCase> AllCases;
        private IEnumerable<TestCase> Cases;
        private string OutputPath;

        public GoogleTestCommandLine(bool runAll, IEnumerable<TestCase> allCases, IEnumerable<TestCase> cases, string outputPath)
        {
            this.RunAll = runAll;
            this.AllCases = allCases;
            this.Cases = cases;
            this.OutputPath = outputPath;
        }

        public string GetCommandLine()
        {
            return string.Join(" ", GetOutputpathParameter(), GetFilterParameter());
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
                    CasesNotHavingCommonSuite.Add(testcase.FullyQualifiedName);
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

    }

}
