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
                foreach (string executable in googleTestExecutables)
                {
                    _settings.ExecuteWithSettingsForExecutable(executable, () =>
                    {
                        int nrOfTestCases = 0;
                        Action<TestCase> reportTestCases = tc =>
                        {
                            reporter.ReportTestsFound(tc.Yield());
                            _logger.DebugInfo("Added testcase " + tc.DisplayName);
                            nrOfTestCases++;
                        };
                        var factory = new TestCaseFactory(executable, _logger, _settings, _diaResolverFactory);
                        factory.CreateTestCases(reportTestCases);
                        _logger.LogInfo("Found " + nrOfTestCases + " tests in executable " + executable);
                    }, _logger);
                }
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
            return allExecutables.Where(
                e => IsGoogleTestExecutable(e, _settings.TestDiscoveryRegex))
                .Select(Path.GetFullPath).ToList();
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