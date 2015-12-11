using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter
{
    [DefaultExecutorUri(GoogleTestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class GoogleTestDiscoverer : ITestDiscoverer
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
        internal class TestCaseInfo
        {
            internal string Suite { get; }

            internal string NameAndParam { get; }

            internal string Name
            {
                get
                {
                    int startOfParamInfo = NameAndParam.IndexOf(GoogleTestConstants.ParameterizedTestMarker);
                    return startOfParamInfo > 0 ? NameAndParam.Substring(0, startOfParamInfo).Trim() : NameAndParam;
                }
            }

            internal string Param
            {
                get
                {
                    int indexOfMarker = NameAndParam.IndexOf(GoogleTestConstants.ParameterizedTestMarker);
                    if (indexOfMarker < 0)
                    {
                        return "";
                    }
                    int startOfParam = indexOfMarker + GoogleTestConstants.ParameterizedTestMarker.Length;
                    return NameAndParam.Substring(startOfParam, NameAndParam.Length - startOfParam).Trim();
                }
            }

            internal TestCaseInfo(string suite, string nameAndParam)
            {
                this.Suite = suite;
                this.NameAndParam = nameAndParam;
            }
        }


        private static readonly Regex CompiledTestFinderRegex = new Regex(Options.TestFinderRegex, RegexOptions.Compiled);


        private TestEnvironment TestEnvironment { get; set; }


        public GoogleTestDiscoverer() : this(null) { }

        public GoogleTestDiscoverer(TestEnvironment testEnvironment)
        {
            this.TestEnvironment = testEnvironment;
        }


        public void DiscoverTests(IEnumerable<string> executables, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            InitTestEnvironment(discoveryContext.RunSettings, logger);

            List<string> googleTestExecutables = GetAllGoogleTestExecutables(executables);
            VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(discoverySink, null, TestEnvironment);
            foreach (string executable in googleTestExecutables)
            {
                List<TestCase2> testCases = GetTestsFromExecutable(executable);
                reporter.ReportTestsFound(testCases);
            }
        }

        public List<TestCase2> GetTestsFromExecutable(string executable)
        {
            List<string> consoleOutput = new ProcessLauncher(TestEnvironment, false).GetOutputOfCommand("", executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, null);
            List<TestCaseInfo> testCaseInfos = ParseTestCases(consoleOutput);
            List<TestCaseLocation> testCaseLocations = GetTestCaseLocations(executable, testCaseInfos);

            TestEnvironment.LogInfo("Found " + testCaseInfos.Count + " tests in executable " + executable);

            List<TestCase2> testCases = new List<TestCase2>();
            foreach (TestCaseInfo testCaseInfo in testCaseInfos)
            {
                testCases.Add(ToTestCase(executable, testCaseInfo, testCaseLocations));
                TestEnvironment.DebugInfo("Added testcase " + testCaseInfo.Suite + "." + testCaseInfo.NameAndParam);
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


        private void InitTestEnvironment(IRunSettings runSettings, IMessageLogger logger)
        {
            if (TestEnvironment == null || TestEnvironment.Options.GetType() == typeof(Options))
            {
                var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                RunSettings ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();

                TestEnvironment = new TestEnvironment(new Options(ourRunSettings, logger), logger);
            }

            TestEnvironment.CheckDebugModeForDiscoveryCode();
        }

        private List<TestCaseInfo> ParseTestCases(List<string> output)
        {
            List<TestCaseInfo> testCaseInfos = new List<TestCaseInfo>();
            string currentSuite = "";
            foreach (string line in output)
            {
                string trimmedLine = line.Trim('.', '\n', '\r');
                if (trimmedLine.StartsWith("  "))
                {
                    testCaseInfos.Add(new TestCaseInfo(currentSuite, trimmedLine.Substring(2)));
                }
                else
                {
                    string[] split = trimmedLine.Split(new[] { GoogleTestConstants.ParameterValueMarker }, StringSplitOptions.RemoveEmptyEntries);
                    currentSuite = split.Length > 0 ? split[0] : trimmedLine;
                }
            }

            return testCaseInfos;
        }

        private List<TestCaseLocation> GetTestCaseLocations(string executable, List<TestCaseInfo> testCaseInfos)
        {
            List<string> testMethodSignatures = testCaseInfos.Select(GetTestMethodSignature).ToList();
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

        private string GetTestMethodSignature(TestCaseInfo testCaseInfo)
        {
            if (!testCaseInfo.NameAndParam.Contains(GoogleTestConstants.ParameterizedTestMarker))
            {
                return GoogleTestConstants.GetTestMethodSignature(testCaseInfo.Suite, testCaseInfo.NameAndParam);
            }

            int index = testCaseInfo.Suite.IndexOf('/');
            string suite = index < 0 ? testCaseInfo.Suite : testCaseInfo.Suite.Substring(index + 1);

            index = testCaseInfo.NameAndParam.IndexOf('/');
            string testName = index < 0 ? testCaseInfo.NameAndParam : testCaseInfo.NameAndParam.Substring(0, index);

            return GoogleTestConstants.GetTestMethodSignature(suite, testName);
        }

        private TestCase2 ToTestCase(string executable, TestCaseInfo testCaseInfo, List<TestCaseLocation> testCaseLocations)
        {
            string fullName = testCaseInfo.Suite + "." + testCaseInfo.NameAndParam;
            string displayName = testCaseInfo.Suite + "." + testCaseInfo.Name;
            if (!string.IsNullOrEmpty(testCaseInfo.Param))
            {
                displayName += $" [{testCaseInfo.Param}]";
            }
            string symbolName = GetTestMethodSignature(testCaseInfo);

            foreach (TestCaseLocation location in testCaseLocations)
            {
                if (location.Symbol.Contains(symbolName))
                {
                    TestCase2 testCase = new TestCase2(fullName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
                    {
                        DisplayName = displayName,
                        CodeFilePath = location.Sourcefile,
                        LineNumber = (int)location.Line
                    };
                    testCase.Traits.AddRange(GetTraits(testCase.DisplayName, location.Traits));
                    return testCase;
                }
            }

            TestEnvironment.LogWarning("Could not find source location for test " + fullName);
            return new TestCase2(fullName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
            {
                DisplayName = displayName
            };
        }

        private IEnumerable<Trait2> GetTraits(string displayName, List<Trait2> traits)
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
                foreach (Trait2 traitToModify in traits.ToArray().Where(T => T.Name == pair.Trait.Name))
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