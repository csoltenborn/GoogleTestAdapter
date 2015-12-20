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
        /*
        Simple tests: 
            Suite=<test_case_name>
            NameAndParam=<test_name>
        Tests with fixture
            Suite=<test_fixture>
            NameAndParam=<test_name>
        Parameterized case: 
            Suite=[<prefix>/]<test_case_name>, 
            NameAndParam=<test_name>/<parameter instantiation nr>  # GetParam() = <parameter instantiation>
        */

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
            List<TestCase> testCases = ParseTestCases(consoleOutput);
            List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(executable, testCases);

            TestEnvironment.LogInfo("Found " + testCases.Count + " tests in executable " + executable);

            foreach (TestCase testCase in testCases)
            {
                ConfigureTestCase(testCase, executable, testCaseLocations);
                TestEnvironment.DebugInfo("Added testcase " + testCase.Suite + "." + testCase.NameAndParam);
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

        private List<TestCase> ParseTestCases(List<string> output)
        {
            List<TestCase> testCases = new List<TestCase>();
            string currentSuite = "";
            foreach (string line in output)
            {
                string trimmedLine = line.Trim('.', '\n', '\r');
                if (trimmedLine.StartsWith("  "))
                {
                    testCases.Add(new TestCase(currentSuite, trimmedLine.Substring(2)));
                }
                else
                {
                    string[] split = trimmedLine.Split(new[] { GoogleTestConstants.ParameterValueMarker }, StringSplitOptions.RemoveEmptyEntries);
                    currentSuite = split.Length > 0 ? split[0] : trimmedLine;
                }
            }

            return testCases;
        }

        private List<TestCaseLocation> GetTestCaseLocations(string executable, List<TestCase> testCases)
        {
            List<string> testMethodSignatures = testCases.Select(tc => tc.GetTestMethodSignature()).ToList();
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

        private void ConfigureTestCase(TestCase testCase, string executable, List<TestCaseLocation> testCaseLocations)
        {
            string fullName = testCase.Suite + "." + testCase.NameAndParam;
            string displayName = testCase.Suite + "." + testCase.Name;
            if (!string.IsNullOrEmpty(testCase.Param))
            {
                displayName += $" [{testCase.Param}]";
            }
            string symbolName = testCase.GetTestMethodSignature();

            foreach (TestCaseLocation location in testCaseLocations)
            {
                if (location.Symbol.Contains(symbolName))
                {
                    testCase.FullyQualifiedName = fullName;
                    testCase.ExecutorUri = new Uri(GoogleTestExecutor.ExecutorUriString);
                    testCase.Source = executable;
                    testCase.DisplayName = displayName;
                    testCase.CodeFilePath = location.Sourcefile;
                    testCase.LineNumber = (int)location.Line;
                    testCase.Traits.AddRange(GetTraits(testCase.DisplayName, location.Traits));
                }
            }

            TestEnvironment.LogWarning("Could not find source location for test " + fullName);
            testCase.FullyQualifiedName = fullName;
            testCase.ExecutorUri = new Uri(GoogleTestExecutor.ExecutorUriString);
            testCase.Source = executable;
            testCase.DisplayName = displayName;
        }

        private IEnumerable<Trait> GetTraits(string displayName, List<Trait> traits)
        {
            foreach (RegexTraitPair pair in TestEnvironment.Options.TraitsRegexesBefore.Where(p => Regex.IsMatch(displayName, p.Regex)))
            {
                if (!traits.Exists(T => T.Name == pair.Trait.Name))
                {
                    traits.Add(pair.Trait);
                }
            }

            foreach (RegexTraitPair pair in TestEnvironment.Options.TraitsRegexesAfter.Where(p => Regex.IsMatch(displayName, p.Regex)))
            {
                bool replacedTrait = false;
                foreach (Trait traitToModify in traits.ToArray().Where(T => T.Name == pair.Trait.Name))
                {
                    replacedTrait = true;
                    traits.Remove(traitToModify);
                    if (!traits.Contains(pair.Trait))
                    {
                        traits.Add(pair.Trait);
                    }
                }
                if (!replacedTrait)
                {
                    traits.Add(pair.Trait);
                }
            }
            return traits;
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