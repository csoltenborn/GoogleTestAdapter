using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter
{
    [DefaultExecutorUri(GoogleTestExecutor.EXECUTOR_URI_STRING)]
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

            List<string> GoogleTestExecutables = GetAllGoogleTestExecutables(executables, logger);
            foreach (string Executable in GoogleTestExecutables)
            {
                List<TestCase> GoogleTestTests = GetTestsFromExecutable(logger, Executable);
                foreach (TestCase TestCase in GoogleTestTests)
                {
                    discoverySink.SendTestCase(TestCase);
                }
            }
        }

        public List<TestCase> GetTestsFromExecutable(IMessageLogger logger, string executable)
        {
            List<string> ConsoleOutput = ProcessUtils.GetOutputOfCommand(logger, "", executable, Constants.gtestListTests, false, false, null, null);
            List<SuiteCasePair> SuiteCasePairs = ParseTestCases(ConsoleOutput);
            SuiteCasePairs.Reverse();
            logger.SendMessage(TestMessageLevel.Informational, "Found " + SuiteCasePairs.Count + " tests, resolving symbols...");
            List<SourceFileLocation> SourceFileLocations = GetSourceFileLocations(executable, logger, SuiteCasePairs);
            List<TestCase> TestCases = new List<TestCase>();
            foreach (SuiteCasePair SuiteCasePair in SuiteCasePairs)
            {
                TestCases.Add(ToTestCase(executable, SuiteCasePair, logger, SourceFileLocations));
                DebugUtils.LogDebugMessage(logger, TestMessageLevel.Informational, "Added testcase " + SuiteCasePair.TestSuite + "." + SuiteCasePair.TestCase);
            }
            return TestCases;
        }

        private List<SuiteCasePair> ParseTestCases(List<string> output)
        {
            List<SuiteCasePair> SuiteCasePairs = new List<SuiteCasePair>();
            string CurrentSuite = "";
            foreach (string Line in output)
            {
                string TrimmedLine = Line.Trim('.', '\n', '\r');
                if (TrimmedLine.StartsWith("  "))
                {
                    SuiteCasePairs.Add(new SuiteCasePair
                    {
                        TestSuite = CurrentSuite,
                        TestCase = TrimmedLine.Substring(2)
                    });
                }
                else
                {
                    string[] Split = TrimmedLine.Split(new[] { ".  # TypeParam" }, StringSplitOptions.RemoveEmptyEntries);
                    CurrentSuite = Split.Length > 0 ? Split[0] : TrimmedLine;
                }
            }

            return SuiteCasePairs;
        }

        private List<SourceFileLocation> GetSourceFileLocations(string executable, IMessageLogger logger, List<SuiteCasePair> testcases)
        {
            List<string> Symbols = testcases.Select(GetGoogleTestCombinedName).ToList();
            string SymbolFilterString = "*" + Constants.gtestTestBodySignature;
            return DiaResolver.ResolveAllMethods(executable, Symbols, SymbolFilterString, logger);
        }

        private string GetGoogleTestCombinedName(SuiteCasePair pair)
        {
            if (!pair.TestCase.Contains("# GetParam()"))
            {
                return pair.TestSuite + "_" + pair.TestCase + "_Test" + Constants.gtestTestBodySignature;
            }

            int Index = pair.TestSuite.IndexOf('/');
            string Suite = Index < 0 ? pair.TestSuite : pair.TestSuite.Substring(Index + 1);

            Index = pair.TestCase.IndexOf('/');
            string TestName = Index < 0 ? pair.TestCase : pair.TestCase.Substring(0, Index);

            return Suite + "_" + TestName + "_Test" + Constants.gtestTestBodySignature;
        }

        private TestCase ToTestCase(string executable, SuiteCasePair suiteCasePair, IMessageLogger logger, List<SourceFileLocation> sourceFileLocations)
        {
            string DisplayName = suiteCasePair.TestSuite + "." + suiteCasePair.TestCase;
            string SymbolName = GetGoogleTestCombinedName(suiteCasePair);

            foreach (SourceFileLocation Location in sourceFileLocations)
            {
                if (Location.Symbol.Contains(SymbolName))
                {
                    TestCase TestCase = new TestCase(DisplayName, new Uri(GoogleTestExecutor.EXECUTOR_URI_STRING), executable)
                    {
                        DisplayName = DisplayName,
                        CodeFilePath = Location.Sourcefile,
                        LineNumber = (int) Location.Line
                    };
                    TestCase.Traits.AddRange(GetTraits(TestCase.FullyQualifiedName, Location.Traits));
                    return TestCase;
                }
            }
            logger.SendMessage(TestMessageLevel.Warning, "Could not find source location for test " + DisplayName);
            return new TestCase(DisplayName, new Uri(GoogleTestExecutor.EXECUTOR_URI_STRING), executable)
            {
                DisplayName = DisplayName
            };
        }

        private IEnumerable<Trait> GetTraits(string fullyQualifiedName, List<Trait> traits)
        {
            foreach (RegexTraitPair Pair in Options.TraitsRegexesBefore.Where(P => Regex.IsMatch(fullyQualifiedName, P.Regex)))
            {
                if (!traits.Exists(T => T.Name == Pair.Trait.Name))
                {
                    traits.Add(Pair.Trait);
                }
            }

            foreach (RegexTraitPair Pair in Options.TraitsRegexesAfter.Where(P => Regex.IsMatch(fullyQualifiedName, P.Regex)))
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
            bool Matches;
            string RegexUsed;
            if (string.IsNullOrWhiteSpace(CustomRegex))
            {
                RegexUsed = Constants.TEST_FINDER_REGEX;
                Matches = COMPILED_TEST_FINDER_REGEX.IsMatch(executable);
            }
            else
            {
                RegexUsed = CustomRegex;
                try
                {
                    Matches = Regex.IsMatch(executable, CustomRegex);
                }
                catch (ArgumentException e)
                {
                    logger.SendMessage(TestMessageLevel.Error,
                        "Google Test Adapter: Regex '" + RegexUsed + "' configured under Options/Google Test Adapter can not be parsed: " + e.Message);
                    Matches = false;
                }
                catch (RegexMatchTimeoutException e)
                {
                    logger.SendMessage(TestMessageLevel.Error,
                        "Google Test Adapter: Regex '" + RegexUsed + "' configured under Options/Google Test Adapter timed out: " + e.Message);
                    Matches = false;
                }
            }

            DebugUtils.LogDebugMessage(logger, TestMessageLevel.Informational,
                    "GoogleTestAdapter: Does " + executable + " match " + RegexUsed + ": " + Matches);

            return Matches;
        }

        private List<string> GetAllGoogleTestExecutables(IEnumerable<string> allExecutables, IMessageLogger logger)
        {
            return allExecutables.AsParallel().Where(e => IsGoogleTestExecutable(e, logger, Options.TestDiscoveryRegex)).ToList();
        }


        class SuiteCasePair
        {
            public string TestSuite;
            public string TestCase;
        }

        public class SourceFileLocation
        {
            public string Symbol;
            public string Sourcefile;
            public uint Line;
            public List<Trait> Traits;
        }

    }
}
