using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

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

        public void DiscoverTests(IEnumerable<string> executables, ILogger logger, ITestFrameworkReporter reporter)
        {
            List<string> googleTestExecutables = GetAllGoogleTestExecutables(executables);
            foreach (string executable in googleTestExecutables)
            {
                List<TestCase> testCases = GetTestsFromExecutable(executable);
                reporter.ReportTestsFound(testCases);
            }
        }

        public List<TestCase> GetTestsFromExecutable(string executable)
        {
            List<string> consoleOutput = new ProcessLauncher(TestEnvironment, false).GetOutputOfCommand("", executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, null);
            List<TestCase> testCases = ParseTestCases(executable, consoleOutput);
            List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(executable, testCases);

            TestEnvironment.LogInfo("Found " + testCases.Count + " tests in executable " + executable);

            foreach (TestCase testCase in testCases)
            {
                testCase.AddLocationInfo(testCaseLocations, TestEnvironment);
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

        private List<TestCase> ParseTestCases(string executable, List<string> output)
        {
            List<TestCase> testCases = new List<TestCase>();
            string currentSuite = "";
            foreach (string line in output)
            {
                string trimmedLine = line.Trim('.', '\n', '\r');
                if (trimmedLine.StartsWith("  "))
                {
                    testCases.Add(new TestCase(executable, currentSuite, trimmedLine.Substring(2)));
                }
                else
                {
                    currentSuite = trimmedLine;
                }
            }

            return testCases;
        }

        private List<TestCaseLocation> GetTestCaseLocations(string executable, List<TestCase> testCases)
        {
            List<string> testMethodSignatures = new List<string>();
            foreach (TestCase testCase in testCases)
            {
                testMethodSignatures.AddRange(testCase.GetTestMethodSignatures());
            }
            string filterString = "*" + GoogleTestConstants.TestBodySignature;
            TestCaseResolver resolver = new TestCaseResolver();
            List<string> errorMessages = new List<string>();
            List<TestCaseLocation> testCaseLocations = resolver.ResolveAllTestCases(executable, testMethodSignatures, filterString, errorMessages);
            foreach (string errorMessage in errorMessages)
            {
                TestEnvironment.LogWarning(errorMessage);
            }
            return testCaseLocations;
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