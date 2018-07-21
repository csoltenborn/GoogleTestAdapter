// This file has been modified by Microsoft on 9/2017.

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
            var discoveryActions = executables
                .Select(e => (Action)(() => DiscoverTests(e, reporter, _settings.Clone(), _logger, _diaResolverFactory)))
                .ToArray();
            Utils.SpawnAndWait(discoveryActions);
        }

        private static void DiscoverTests(string executable, ITestFrameworkReporter reporter, SettingsWrapper settings, ILogger logger, IDiaResolverFactory diaResolverFactory)
        {
            settings.ExecuteWithSettingsForExecutable(executable, () =>
            {
                if (!VerifyExecutableTrust(executable, settings, logger)
                    || !IsGoogleTestExecutable(executable, settings.TestDiscoveryRegex, logger))
                    return;

                int nrOfTestCases = 0;
                Action<TestCase> reportTestCases = tc =>
                {
                    reporter.ReportTestsFound(tc.Yield());
                    logger.DebugInfo(String.Format(Resources.AddedTestCase, tc.DisplayName));
                    nrOfTestCases++;
                };
                var factory = new TestCaseFactory(executable, logger, settings, diaResolverFactory);
                factory.CreateTestCases(reportTestCases);
                logger.LogInfo(String.Format(Resources.NumberOfTestsMessage, nrOfTestCases, executable));
            }, logger);
        }

        public IList<TestCase> GetTestsFromExecutable(string executable)
        {
            var factory = new TestCaseFactory(executable, _logger, _settings, _diaResolverFactory);
            IList<TestCase> testCases = factory.CreateTestCases();

            foreach (TestCase testCase in testCases)
            {
                _logger.DebugInfo(String.Format(Resources.AddedTestCase, testCase.DisplayName));
            }
            _logger.LogInfo(String.Format(Resources.NumberOfTestsMessage, testCases.Count, executable));

            return testCases;
        }

        public static bool IsGoogleTestExecutable(string executable, string customRegex, ILogger logger)
        {
            string googleTestIndicatorFile = $"{executable}{GoogleTestIndicator}";
            if (File.Exists(googleTestIndicatorFile))
            {
                logger.DebugInfo(String.Format(Resources.FileFound, executable));
                return true;
            }

            if (string.IsNullOrWhiteSpace(customRegex))
            {
                if (PeParser.FindImport(executable, GoogleTestConstants.GoogleTestDllMarker, StringComparison.OrdinalIgnoreCase, logger)
                    || Utils.BinaryFileContainsStrings(executable, Encoding.ASCII, GoogleTestConstants.GoogleTestExecutableMarkers))
                {
                    logger.DebugInfo($"Google Test indicators found in executable {executable}");
                    return true;
                }
            }
            else
            {
                if (SafeMatches(executable, customRegex, logger))
                {
                    logger.DebugInfo(String.Format(Resources.MatchesCustom, executable, customRegex));
                    return true;
                }
            }

            logger.DebugInfo(String.Format(Resources.FileNotFound, executable));
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
                logger.LogError(String.Format(Resources.RegexParseError, regex, e.Message));
            }
            catch (RegexMatchTimeoutException e)
            {
                logger.LogError(String.Format(Resources.RegexTimedOut, regex, e.Message));
            }
            return matches;
        }

        public static bool VerifyExecutableTrust(string executable, SettingsWrapper settings, ILogger logger)
        {
            if (settings.SkipOriginCheck)
                return true;

            var zone = Zone.CreateFromUrl(Path.GetFullPath(executable));
            if (zone.SecurityZone != System.Security.SecurityZone.MyComputer)
            {
                logger.LogError(String.Format(Resources.ExecutableError, executable));
                return false;
            }
            return true;
        }

    }

}