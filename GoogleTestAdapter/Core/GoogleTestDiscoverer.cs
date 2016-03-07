using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.TestCases;

namespace GoogleTestAdapter
{
    public class GoogleTestDiscoverer
    {
        private static readonly Regex CompiledTestFinderRegex = new Regex(Options.TestFinderRegex, RegexOptions.Compiled);

        private TestEnvironment TestEnvironment { get; }

        public GoogleTestDiscoverer(TestEnvironment testEnviroment)
        {
            TestEnvironment = testEnviroment;
        }

        public void DiscoverTests(IEnumerable<string> executables, ITestFrameworkReporter reporter)
        {
            List<string> googleTestExecutables = GetAllGoogleTestExecutables(executables);
            foreach (string executable in googleTestExecutables)
            {
                IList<TestCase> testCases = GetTestsFromExecutable(executable);
                reporter.ReportTestsFound(testCases);
            }
        }

        public IList<TestCase> GetTestsFromExecutable(string executable)
        {
            TestCaseFactory factory = new TestCaseFactory(executable, TestEnvironment);
            IList<TestCase> testCases = factory.CreateTestCases();

            TestEnvironment.LogInfo("Found " + testCases.Count + " tests in executable " + executable);
            foreach (TestCase testCase in testCases)
            {
                TestEnvironment.DebugInfo("Added testcase " + testCase.DisplayName);
            }

            return testCases;
        }

        public bool IsGoogleTestExecutable(string executable, string customRegex = "")
        {
            bool matches;
            string regexUsed;
            if (string.IsNullOrWhiteSpace(customRegex))
            {
                regexUsed = Options.TestFinderRegex;
                matches = CompiledTestFinderRegex.IsMatch(executable);
            }
            else
            {
                regexUsed = customRegex;
                matches = SafeMatches(executable, customRegex);
            }

            TestEnvironment.DebugInfo(
                    executable + (matches ? " matches " : " does not match ") + "regex '" + regexUsed + "'");

            return matches;
        }

        private List<string> GetAllGoogleTestExecutables(IEnumerable<string> allExecutables)
        {
            return allExecutables.Where(e => IsGoogleTestExecutable(e, TestEnvironment.Options.TestDiscoveryRegex)).ToList();
        }

        private bool SafeMatches(string executable, string regex)
        {
            bool matches = false;
            try
            {
                matches = Regex.IsMatch(executable, regex);
            }
            catch (ArgumentException e)
            {
                TestEnvironment.LogError($"Regex '{regex}' can not be parsed: {e.Message}");
            }
            catch (RegexMatchTimeoutException e)
            {
                TestEnvironment.LogError($"Regex '{regex}' timed out: {e.Message}");
            }
            return matches;
        }

    }

}