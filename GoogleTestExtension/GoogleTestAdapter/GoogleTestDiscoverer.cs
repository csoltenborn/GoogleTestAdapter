using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using GoogleTestAdapter.Dia;
using GoogleTestAdapter.Helpers;

using SourceFileLocation = GoogleTestAdapter.Dia.DiaResolver.SourceFileLocation;

namespace GoogleTestAdapter
{
    [DefaultExecutorUri(GoogleTestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class GoogleTestDiscoverer : ITestDiscoverer
    {

        /*
        Simple tests: 
            TestSuite=<test_case_name>
            TestCase=<test_name>
        Tests with fixture
            TestSuite=<test_fixture>
            TestCase=<test_name>
        Parameterized case: 
            TestSuite=[<prefix>/]<test_case_name>, 
            TestCase=<test_name>/<parameter instantiation nr>  # GetParam() = <parameter instantiation>
        */
        private class SuiteTestCasePair
        {
            internal string TestSuite { get; }
            internal string TestCase { get; }

            internal SuiteTestCasePair(string testSuite, string testCase)
            {
                this.TestSuite = testSuite;
                this.TestCase = testCase;
            }
        }


        public const string TestFinderRegex = @"[Tt]est[s]?\.exe";

        private static readonly Regex CompiledTestFinderRegex = new Regex(TestFinderRegex, RegexOptions.Compiled);


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
            VsTestFrameworkReporter reporter = new VsTestFrameworkReporter(TestEnvironment);
            foreach (string executable in googleTestExecutables)
            {
                List<TestCase> testCases = GetTestsFromExecutable(executable);
                reporter.ReportTestsFound(discoverySink, testCases);
            }
        }

        public List<TestCase> GetTestsFromExecutable(string executable)
        {
            List<string> consoleOutput = new ProcessLauncher(TestEnvironment).GetOutputOfCommand("", executable, GoogleTestConstants.ListTestsOption.Trim(), false, false, null, null);
            List<SuiteTestCasePair> suiteTestCasePairs = ParseTestCases(consoleOutput);
            List<SourceFileLocation> sourceFileLocations = GetSourceFileLocations(executable, suiteTestCasePairs);

            TestEnvironment.LogInfo("Found " + suiteTestCasePairs.Count + " tests in executable " + executable);

            List<TestCase> testCases = new List<TestCase>();
            foreach (SuiteTestCasePair suiteCasePair in suiteTestCasePairs)
            {
                testCases.Add(ToTestCase(executable, suiteCasePair, sourceFileLocations));
                TestEnvironment.LogInfo("Added testcase " + suiteCasePair.TestSuite + "." + suiteCasePair.TestCase, TestEnvironment.LogType.Debug);
            }
            return testCases;
        }

        public bool IsGoogleTestExecutable(string executable, string customRegex = "")
        {
            bool matches;
            string regexUsed;
            if (string.IsNullOrWhiteSpace(customRegex))
            {
                regexUsed = TestFinderRegex;
                matches = CompiledTestFinderRegex.IsMatch(executable);
            }
            else
            {
                regexUsed = customRegex;
                matches = SafeMatches(executable, customRegex);
            }

            TestEnvironment.LogInfo(
                    executable + (matches ? " matches " : " does not match ") + "regex '" + regexUsed + "'", TestEnvironment.LogType.UserDebug);

            return matches;
        }


        private void InitTestEnvironment(IRunSettings runSettings, IMessageLogger logger)
        {
            if (TestEnvironment == null)
            {
                RunSettings ourRunSettings;
                var settingsProvider = runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
                ourRunSettings = settingsProvider != null ? settingsProvider.Settings : new RunSettings();

                TestEnvironment = new TestEnvironment(new Options(ourRunSettings, logger), logger);
            }

            TestEnvironment.CheckDebugModeForDiscoveryCode();
        }

        private List<SuiteTestCasePair> ParseTestCases(List<string> output)
        {
            List<SuiteTestCasePair> suiteCasePairs = new List<SuiteTestCasePair>();
            string currentSuite = "";
            foreach (string line in output)
            {
                string trimmedLine = line.Trim('.', '\n', '\r');
                if (trimmedLine.StartsWith("  "))
                {
                    suiteCasePairs.Add(new SuiteTestCasePair(currentSuite, trimmedLine.Substring(2)));
                }
                else
                {
                    string[] split = trimmedLine.Split(new[] { GoogleTestConstants.ParameterValueMarker }, StringSplitOptions.RemoveEmptyEntries);
                    currentSuite = split.Length > 0 ? split[0] : trimmedLine;
                }
            }

            return suiteCasePairs;
        }

        private List<SourceFileLocation> GetSourceFileLocations(string executable, List<SuiteTestCasePair> testcases)
        {
            List<string> testMethodSignatures = testcases.Select(GetTestMethodSignature).ToList();
            string filterString = "*" + GoogleTestConstants.TestBodySignature;
            DiaResolver resolver = new DiaResolver(TestEnvironment);
            return resolver.ResolveAllMethods(executable, testMethodSignatures, filterString);
        }

        private string GetTestMethodSignature(SuiteTestCasePair pair)
        {
            if (!pair.TestCase.Contains(GoogleTestConstants.ParameterizedTestMarker))
            {
                return GoogleTestConstants.GetTestMethodSignature(pair.TestSuite, pair.TestCase);
            }

            int index = pair.TestSuite.IndexOf('/');
            string suite = index < 0 ? pair.TestSuite : pair.TestSuite.Substring(index + 1);

            index = pair.TestCase.IndexOf('/');
            string testName = index < 0 ? pair.TestCase : pair.TestCase.Substring(0, index);

            return GoogleTestConstants.GetTestMethodSignature(suite, testName);
        }

        private TestCase ToTestCase(string executable, SuiteTestCasePair suiteCasePair, List<SourceFileLocation> sourceFileLocations)
        {
            string displayName = suiteCasePair.TestSuite + "." + suiteCasePair.TestCase;
            string symbolName = GetTestMethodSignature(suiteCasePair);

            foreach (SourceFileLocation location in sourceFileLocations)
            {
                if (location.Symbol.Contains(symbolName))
                {
                    TestCase testCase = new TestCase(displayName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
                    {
                        DisplayName = displayName,
                        CodeFilePath = location.Sourcefile,
                        LineNumber = (int)location.Line
                    };
                    testCase.Traits.AddRange(GetTraits(testCase.FullyQualifiedName, location.Traits));
                    return testCase;
                }
            }

            TestEnvironment.LogWarning("Could not find source location for test " + displayName);
            return new TestCase(displayName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
            {
                DisplayName = displayName
            };
        }

        private IEnumerable<Trait> GetTraits(string fullyQualifiedName, List<Trait> traits)
        {
            foreach (RegexTraitPair pair in TestEnvironment.Options.TraitsRegexesBefore.Where(p => Regex.IsMatch(fullyQualifiedName, p.Regex)))
            {
                if (!traits.Exists(T => T.Name == pair.Trait.Name))
                {
                    traits.Add(pair.Trait);
                }
            }

            foreach (RegexTraitPair pair in TestEnvironment.Options.TraitsRegexesAfter.Where(p => Regex.IsMatch(fullyQualifiedName, p.Regex)))
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
                TestEnvironment.LogError(
                    "Regex '" + regex + "' configured under Options/Google Test Adapter can not be parsed: " + e.Message);
            }
            catch (RegexMatchTimeoutException e)
            {
                TestEnvironment.LogError(
                    "Regex '" + regex + "' configured under Options/Google Test Adapter timed out: " + e.Message);
            }
            return matches;
        }

    }

}