using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter
{
    [DefaultExecutorUri(GoogleTestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class GoogleTestDiscoverer : AbstractGoogleTestAdapterClass, ITestDiscoverer
    {
        private static readonly Regex COMPILED_TEST_FINDER_REGEX = new Regex(Constants.TEST_FINDER_REGEX, RegexOptions.Compiled);

        private static bool ProcessIdShown = false;

        public GoogleTestDiscoverer() : this(null) {}

        public GoogleTestDiscoverer(IOptions options) : base(options) {}

        public void DiscoverTests(IEnumerable<string> executables, IDiscoveryContext discoveryContext, 
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            if (!ProcessIdShown)
            {
                ProcessIdShown = true;
                DebugUtils.CheckDebugModeForDiscoverageCode(logger);
            }

            List<string> googleTestExecutables = GetAllGoogleTestExecutables(executables, logger);
            foreach (string executable in googleTestExecutables)
            {
                List<TestCase> googleTestTests = GetTestsFromExecutable(logger, executable);
                foreach (TestCase test in googleTestTests)
                {
                    discoverySink.SendTestCase(test);
                }
            }
        }

        public List<TestCase> GetTestsFromExecutable(IMessageLogger logger, string executable)
        {
            List<string> output = ProcessUtils.GetOutputOfCommand(logger, "", executable, Constants.gtestListTests, false, false);
            List<SuiteCasePair> testcases = ParseTestCases(output);
            testcases.Reverse();
            logger.SendMessage(TestMessageLevel.Informational, "Found " + testcases.Count + " tests, resolving symbols...");
            List<SourceFileLocation> sourceFileLocations = GetSourceFileLocations(executable, logger, testcases);
            List<TestCase> result = new List<TestCase>();
            foreach (SuiteCasePair testcase in testcases)
            {
                result.Add(ToTestCase(executable, testcase, logger, sourceFileLocations));
                DebugUtils.LogDebugMessage(logger, TestMessageLevel.Informational, "Added testcase" + testcase.testsuite + "." + testcase.testcase);
            }
            return result;
        }

        private List<SuiteCasePair> ParseTestCases(List<string> output)
        {
            List<SuiteCasePair> Result = new List<SuiteCasePair>();
            string currentSuite = "";
            foreach (string Line in output)
            {
                string TrimmedLine = Line.Trim('.', '\n', '\r');
                if (TrimmedLine.StartsWith("  "))
                {
                    Result.Add(new SuiteCasePair
                    {
                        testsuite = currentSuite,
                        testcase = TrimmedLine.Substring(2)
                    });
                }
                else
                {
                    string[] split = TrimmedLine.Split(new[] { ".  # TypeParam" }, StringSplitOptions.RemoveEmptyEntries);
                    currentSuite = split.Length > 0 ? split[0] : TrimmedLine;
                }
            }

            return Result;
        }

        private List<SourceFileLocation> GetSourceFileLocations(string executable, IMessageLogger logger, List<SuiteCasePair> testcases)
        {
            List<string> symbols = testcases.Select(GoogleTestCombinedName).ToList();
            string symbolFilterString = "*" + Constants.gtestTestBodySignature;
            return DiaResolver.ResolveAllMethods(executable, symbols, symbolFilterString, logger);
        }

        private string GoogleTestCombinedName(SuiteCasePair pair)
        {
            if (!pair.testcase.Contains("# GetParam()"))
            {
                return pair.testsuite + "_" + pair.testcase + "_Test" + Constants.gtestTestBodySignature;
            }

            int Index = pair.testsuite.IndexOf('/');
            string suite = Index < 0 ? pair.testsuite : pair.testsuite.Substring(Index + 1);

            Index = pair.testcase.IndexOf('/');
            string testname = Index < 0 ? pair.testcase : pair.testcase.Substring(0, Index);

            return suite + "_" + testname + "_Test" + Constants.gtestTestBodySignature;
        }

        private TestCase ToTestCase(string executable, SuiteCasePair testcase, IMessageLogger logger, List<SourceFileLocation> sourceFileLocations)
        {
            string displayName = testcase.testsuite + "." + testcase.testcase;
            string symbolName = GoogleTestCombinedName(testcase);

            foreach (SourceFileLocation location in sourceFileLocations)
            {
                if (location.symbol.Contains(symbolName))
                {
                    TestCase TestCase = new TestCase(displayName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
                    {
                        DisplayName = displayName,
                        CodeFilePath = location.sourcefile,
                        LineNumber = (int) location.line
                    };
                    TestCase.Traits.AddRange(GetTraits(TestCase.FullyQualifiedName, location.traits));
                    return TestCase;
                }
            }
            logger.SendMessage(TestMessageLevel.Warning, "Could not find source location for test " + displayName);
            return new TestCase(displayName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
            {
                DisplayName = displayName
            };
        }

        private IEnumerable<Trait> GetTraits(string fullyQualifiedName, List<Trait> traits)
        {
            foreach (RegexTraitPair Pair in Options.TraitsRegexes.Where(P => Regex.IsMatch(fullyQualifiedName, P.Regex)))
            {
                bool ReplacedTrait = false;
                foreach (Trait TraitToModify in traits.ToArray().Where(T => T.Name == Pair.Trait.Name))
                {
                    ReplacedTrait = true;
                    traits.Remove(TraitToModify);
                    if (!traits.Contains(Pair.Trait))
                    {
                        traits.Add(Pair.Trait);
                    }
                }
                if (!ReplacedTrait)
                {
                    traits.Add(Pair.Trait);
                }
            }
            return traits;
        }

        public static bool IsGoogleTestExecutable(string executable, IMessageLogger logger, string CustomRegex = "")
        {
            bool matches;
            string regexUsed;
            if (string.IsNullOrWhiteSpace(CustomRegex))
            {
                regexUsed = Constants.TEST_FINDER_REGEX;
                matches = COMPILED_TEST_FINDER_REGEX.IsMatch(executable);
            }
            else
            {
                regexUsed = CustomRegex;
                try
                {
                    matches = Regex.IsMatch(executable, CustomRegex);
                }
                catch (ArgumentException e)
                {
                    logger.SendMessage(TestMessageLevel.Error,
                        "Google Test Adapter: Regex '" + regexUsed + "' configured under Options/Google Test Adapter can not be parsed: " + e.Message);
                    matches = false;
                }
                catch (RegexMatchTimeoutException e)
                {
                    logger.SendMessage(TestMessageLevel.Error,
                        "Google Test Adapter: Regex '" + regexUsed + "' configured under Options/Google Test Adapter timed out: " + e.Message);
                    matches = false;
                }
            }

            DebugUtils.LogDebugMessage(logger, TestMessageLevel.Informational,
                    "GoogleTestAdapter: Does " + executable + " match " + regexUsed + ": " + matches);

            return matches;
        }

        private List<string> GetAllGoogleTestExecutables(IEnumerable<string> allExecutables, IMessageLogger logger)
        {
            return allExecutables.AsParallel().Where(e => IsGoogleTestExecutable(e, logger, Options.TestDiscoveryRegex)).ToList();
        }


        class SuiteCasePair
        {
            public string testsuite;
            public string testcase;
        }

        public class SourceFileLocation
        {
            public string symbol;
            public string sourcefile;
            public uint line;
            public List<Trait> traits;
        }

    }
}
