// This file has been modified by Microsoft on 7/2017.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
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
        public const string GoogleTestIndicator = ".is_google_test";

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
            if (_settings.UseNewTestExecutionFramework)
            {
                var discoveryActions = executables
                    .Select(e => (Action)(() => DiscoverTests(e, reporter, _settings.Clone(), _logger, _diaResolverFactory)))
                    .ToArray();
                Utils.SpawnAndWait(discoveryActions);
            }
            else
            {
                foreach (string executable in executables)
                {
                    _settings.ExecuteWithSettingsForExecutable(executable, () =>
                    {
                        if (VerifyExecutableTrust(executable, _logger) && IsGoogleTestExecutable(executable, _settings.TestDiscoveryRegex, _logger))
                        {
                            IList<TestCase> testCases = GetTestsFromExecutable(executable);
                            reporter.ReportTestsFound(testCases);
                        }
                    }, _logger);
                }
            }
        }

        private static void DiscoverTests(string executable, ITestFrameworkReporter reporter, SettingsWrapper settings, ILogger logger, IDiaResolverFactory diaResolverFactory)
        {
            settings.ExecuteWithSettingsForExecutable(executable, () =>
            {
                if (!VerifyExecutableTrust(executable, logger) || !IsGoogleTestExecutable(executable, settings.TestDiscoveryRegex, logger))
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

        public static bool IsGoogleTestExecutable(string executable, string customRegex, ILogger logger)
        {
            string googleTestIndicatorFile = $"{executable}{GoogleTestIndicator}";
            if (File.Exists(googleTestIndicatorFile))
            {
                logger.DebugInfo($"Google Test indicator file found for executable {executable} ({googleTestIndicatorFile})");
                return true;
            }

            if (string.IsNullOrWhiteSpace(customRegex))
            {
                if (Utils.BinaryFileContainsStrings(executable, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers))
                {
                    logger.DebugInfo($"Google Test indicators found in executable {executable}");
                    return true;
                }
            }
            else
            {
                if (SafeMatches(executable, customRegex, logger))
                {
                    logger.DebugInfo($"Custom regex '{customRegex}' matches executable '{executable}'");
                    return true;
                }
            }

            logger.DebugInfo($"File does not seem to be Google Test executable: '{executable}'");
            return false;
        }

        private static bool SafeMatches(string executable, string regex, ILogger logger)
        {
            bool matches = false;
            try
            {
                matches = Regex.IsMatch(executable, regex);
            }
            catch (ArgumentException e)
            {
                logger.LogError($"Regex '{regex}' can not be parsed: {e.Message}");
            }
            catch (RegexMatchTimeoutException e)
            {
                logger.LogError($"Regex '{regex}' timed out: {e.Message}");
            }
            return matches;
        }

        public static bool VerifyExecutableTrust(string executable, ILogger logger)
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