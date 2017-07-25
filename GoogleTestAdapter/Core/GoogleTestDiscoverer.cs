// This file has been modified by Microsoft on 7/2017.

using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestCases;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter
{
    public class GoogleTestDiscoverer
    {
        public const string GoogleTestIndicator = ".is_google_test";

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
                        if (!VerifyExecutableTrust(executable, _logger))
                            return;

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
                if (!VerifyExecutableTrust(executable, logger))
                    return;

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
            string googleTestIndicatorFile = $"{executable}{GoogleTestIndicator}";
            if (File.Exists(googleTestIndicatorFile))
            {
                _logger.DebugInfo($"Google Test indicator file found for executable {executable}");
                return true;
            }
            _logger.DebugInfo($"No Google Test indicator file found for executable {executable}");

            bool matches;
            string regexSource, regex;
            if (string.IsNullOrWhiteSpace(customRegex))
            {
                regexSource = "default";
                regex = SettingsWrapper.TestFinderRegex;
                matches = CompiledTestFinderRegex.IsMatch(executable);
            }
            else
            {
                regexSource = "custom";
                regex = customRegex;
                matches = SafeMatches(executable, customRegex);
            }
            _logger.DebugInfo(
                $"'{executable}' {(matches ? "matches" : "does not match")} {regexSource} regex '{regex}'");

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

        private static bool VerifyExecutableTrust(string executable, ILogger logger)
        {
            var zone = Zone.CreateFromUrl(executable);
            if (zone.SecurityZone != System.Security.SecurityZone.MyComputer)
            {
                logger.LogError("Executable " + executable + " came from another computer and was blocked to help protect this computer.");
                return false;
            }
            return true;
        }

    }

}