using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        private readonly TestEnvironment _testEnvironment;
        private readonly IDiaResolverFactory _diaResolverFactory;

        public GoogleTestDiscoverer(TestEnvironment testEnvironment, IDiaResolverFactory diaResolverFactory = null)
        {
            _testEnvironment = testEnvironment;
            _diaResolverFactory = diaResolverFactory ?? DefaultDiaResolverFactory.Instance;
        }

        public void DiscoverTests(IEnumerable<string> executables, ITestFrameworkReporter reporter)
        {
            IList<string> googleTestExecutables = GetAllGoogleTestExecutables(executables);
            if (_testEnvironment.Options.UseNewTestExecutionFramework)
            {
                foreach (string executable in googleTestExecutables)
                {
                    _testEnvironment.Options.ExecuteWithSettingsForExecutable(executable, () =>
                    {
                        int nrOfTestCases = 0;
                        Action<TestCase> reportTestCases = tc =>
                        {
                            reporter.ReportTestsFound(tc.Yield());
                            _testEnvironment.DebugInfo("Added testcase " + tc.DisplayName);
                            nrOfTestCases++;
                        };
                        var factory = new TestCaseFactory(executable, _testEnvironment, _diaResolverFactory);
                        factory.CreateTestCases(reportTestCases);
                        _testEnvironment.LogInfo("Found " + nrOfTestCases + " tests in executable " + executable);
                    }, _testEnvironment);
                }
            }
            else
            {
                foreach (string executable in googleTestExecutables)
                {
                    _testEnvironment.Options.ExecuteWithSettingsForExecutable(executable, () =>
                    {
                        IList<TestCase> testCases = GetTestsFromExecutable(executable);
                        reporter.ReportTestsFound(testCases);
                    }, _testEnvironment);
                }
            }
        }

        public IList<TestCase> GetTestsFromExecutable(string executable)
        {
            var factory = new TestCaseFactory(executable, _testEnvironment, _diaResolverFactory);
            IList<TestCase> testCases = factory.CreateTestCases();

            foreach (TestCase testCase in testCases)
            {
                _testEnvironment.DebugInfo("Added testcase " + testCase.DisplayName);
            }
            _testEnvironment.LogInfo("Found " + testCases.Count + " tests in executable " + executable);

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

            _testEnvironment.DebugInfo(
                    executable + (matches ? " matches " : " does not match ") + "regex '" + regexUsed + "'");

            return matches;
        }

        private IList<string> GetAllGoogleTestExecutables(IEnumerable<string> allExecutables)
        {
            return allExecutables.Where(
                e => IsGoogleTestExecutable(e, _testEnvironment.Options.TestDiscoveryRegex))
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
                _testEnvironment.LogError($"Regex '{regex}' can not be parsed: {e.Message}");
            }
            catch (RegexMatchTimeoutException e)
            {
                _testEnvironment.LogError($"Regex '{regex}' timed out: {e.Message}");
            }
            return matches;
        }

    }

}