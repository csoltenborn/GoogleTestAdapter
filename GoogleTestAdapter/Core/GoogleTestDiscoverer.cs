using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestCases;

namespace GoogleTestAdapter
{
    public class GoogleTestDiscoverer
    {
        private static readonly Regex CompiledTestFinderRegex = new Regex(SettingsWrapper.TestFinderRegex, RegexOptions.Compiled);

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly IDiaResolverFactory _diaResolverFactory;

        public GoogleTestDiscoverer(ILogger logger, SettingsWrapper settings, IDiaResolverFactory diaResolverFactory = null)
        {
            _logger = logger;
            _settings = settings;
            _diaResolverFactory = diaResolverFactory ?? DefaultDiaResolverFactory.Instance;
        }

        public void DiscoverTests(IEnumerable<string> executables, ITestFrameworkReporter reporter)
        {
            IList<string> googleTestExecutables = GetAllGoogleTestExecutables(executables);
            if (_settings.UseNewTestExecutionFramework)
            {
                var discoveryActions = googleTestExecutables
                    .Select(e => (Action)(() => DiscoverTests(e, reporter, _settings.Clone(), _logger, _diaResolverFactory)))
                    .ToArray();
                Utils.SpawnAndWait(discoveryActions);
            }
            else
            {
                foreach (string executable in googleTestExecutables)
                {
                    _settings.ExecuteWithSettingsForExecutable(executable, () =>
                    {
                        IList<TestCase> testCases = GetTestsFromExecutable(executable);
                        reporter.ReportTestsFound(testCases);
                    }, _logger);
                }
            }
        }

        private static void DiscoverTests(string executable, ITestFrameworkReporter reporter, SettingsWrapper settings, ILogger logger, IDiaResolverFactory diaResolverFactory)
        {
            settings.ExecuteWithSettingsForExecutable(executable, () =>
            {
                int nrOfTestCases = 0;
                Action<TestCase> reportTestCases = tc =>
                {
                    reporter.ReportTestsFound(tc.Yield());
                    logger.DebugInfo("Added testcase " + tc.DisplayName);
                    nrOfTestCases++;
                };
                var factory = new TestCaseFactory(executable, logger, settings, diaResolverFactory);
                factory.CreateTestCases(reportTestCases);
                logger.LogInfo("Found " + nrOfTestCases + " tests in executable " + executable);
            }, logger);
        }

        public IList<TestCase> GetTestsFromExecutable(string executable)
        {
            var factory = new TestCaseFactory(executable, _logger, _settings, _diaResolverFactory);
            IList<TestCase> testCases = factory.CreateTestCases();

            foreach (TestCase testCase in testCases)
            {
                _logger.DebugInfo("Added testcase " + testCase.DisplayName);
            }
            _logger.LogInfo("Found " + testCases.Count + " tests in executable " + executable);

            return testCases;
        }

        public bool IsGoogleTestExecutable(string executable, string customRegex = "")
        {
            bool matches;
            string regexUsed;
            if (string.IsNullOrWhiteSpace(customRegex))
            {
                regexUsed = SettingsWrapper.TestFinderRegex;
                matches = CompiledTestFinderRegex.IsMatch(executable);
            }
            else
            {
                regexUsed = customRegex;
                matches = SafeMatches(executable, customRegex);
            }

            _logger.DebugInfo(
                    executable + (matches ? " matches " : " does not match ") + "regex '" + regexUsed + "'");

            return matches;
        }

        private IList<string> GetAllGoogleTestExecutables(IEnumerable<string> allExecutables)
        {
            IList<string> testExecutables = new List<string>();
            foreach (string executable in allExecutables)
            {
                _settings.ExecuteWithSettingsForExecutable(executable, () =>
                {
                    if (IsGoogleTestExecutable(executable, _settings.TestDiscoveryRegex))
                        testExecutables.Add(Path.GetFullPath(executable));
                }, _logger);
            }
            return testExecutables;
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
                _logger.LogError($"Regex '{regex}' can not be parsed: {e.Message}");
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.LogError($"Regex '{regex}' timed out: {e.Message}");
            }
            return matches;
        }

    }

}