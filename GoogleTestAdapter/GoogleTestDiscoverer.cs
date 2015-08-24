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
    public class GoogleTestDiscoverer : ITestDiscoverer
    {
        private static Regex COMPILED_TEST_FINDER_REGEX = new Regex(Constants.TEST_FINDER_REGEX, RegexOptions.Compiled);

        private static bool ProcessIdShown = false;

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
            List<string> output = ProcessUtils.GetOutputOfCommand(logger, "", executable, Constants.gtestListTests, false);
            List<SuiteCasePair> testcases = ParseTestCases(output);
            testcases.Reverse();
            logger.SendMessage(TestMessageLevel.Informational, "Found " + testcases.Count + " tests, resolving symbols...");
            List<SourceFileLocation> sourceFileLocations = GetSourceFileLocations(executable, logger, testcases);
            List<TestCase> result = new List<TestCase>();
            foreach (SuiteCasePair testcase in testcases)
            {
                result.Add(ToTestCase(executable, testcase, logger, sourceFileLocations));
                if (Constants.DEBUG_MODE)
                {
                    #pragma warning disable 0162
                    logger.SendMessage(TestMessageLevel.Informational, "Added testcase" + testcase.testsuite + "." + testcase.testcase);
                    #pragma warning restore 0162
                }
            }
            return result;
        }

        private List<SuiteCasePair> ParseTestCases(List<string> output)
        {
            List<SuiteCasePair> Result = new List<SuiteCasePair>();
            string currentSuite = "";
            for (int i = 0; i < output.Count; i++)
            {
                string currentLine = output[i].Trim('.', '\n', '\r');
                if (currentLine.StartsWith("  "))
                {
                    Result.Add(new SuiteCasePair
                    {
                        testsuite = currentSuite,
                        testcase = currentLine.Substring(2)
                    });
                }
                else
                {
                    string[] split = currentLine.Split(new string[] { ".  # TypeParam" }, StringSplitOptions.RemoveEmptyEntries);
                    currentSuite = split.Length > 0 ? split[0] : currentLine;
                }
            }

            return Result;
        }

        private List<SourceFileLocation> GetSourceFileLocations(string executable, IMessageLogger logger, List<SuiteCasePair> testcases)
        {
            List<string> symbols = new List<string>();
            foreach (SuiteCasePair pair in testcases)
            {
                symbols.Add(GoogleTestCombinedName(pair));
            }
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
                    return new TestCase(displayName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
                    {
                        DisplayName = displayName,
                        CodeFilePath = location.sourcefile,
                        LineNumber = (int) location.line
                    };
                }
            }
            logger.SendMessage(TestMessageLevel.Warning, "Could not find source location for test " + displayName);
            return new TestCase(displayName, new Uri(GoogleTestExecutor.ExecutorUriString), executable)
            {
                DisplayName = displayName
            };
        }

        public static bool IsGoogleTestExecutable(string executable, IMessageLogger logger)
        {
            string CustomRegex = Options.TestDiscoveryRegex();
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

            if (Constants.DEBUG_MODE)
            {
                #pragma warning disable 0162
                logger.SendMessage(TestMessageLevel.Informational,
                    "GoogleTestAdapter: Does " + executable + " match " + regexUsed + ": " + matches);
                #pragma warning restore 0162
            }

            return matches;
        }

        private List<string> GetAllGoogleTestExecutables(IEnumerable<string> allExecutables, IMessageLogger logger)
        {
            List<string> googleTestExecutables = new List<string>();
            foreach (string executable in allExecutables.Where(e => IsGoogleTestExecutable(e, logger)))
            {
                 googleTestExecutables.Add(executable);
            }
            return googleTestExecutables;
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
        }

    }
}
