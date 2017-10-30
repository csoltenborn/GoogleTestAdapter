// This file has been modified by Microsoft on 8/2017.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.Helpers
{

    public class TestCaseFilter
    {
        private static readonly Regex TraitValueRegex = new Regex(@"^[\w$]+$", RegexOptions.Compiled);

        private readonly IRunContext _runContext;
        private readonly ILogger _logger;

        private readonly IDictionary<string, TestProperty> _testPropertiesMap = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, TestProperty> _traitPropertiesMap = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);

        private readonly ISet<string> _traitPropertyNames;
        private readonly ISet<string> _allPropertyNames;

        public TestCaseFilter(IRunContext runContext, ISet<string> traitNames, ILogger logger)
        {
            _runContext = runContext;
            _logger = logger;

            InitProperties(traitNames);

            _traitPropertyNames = new HashSet<string>(_traitPropertiesMap.Keys);
            _allPropertyNames = new HashSet<string>(_testPropertiesMap.Keys.Union(_traitPropertyNames));
        }

        public IEnumerable<TestCase> Filter(IEnumerable<TestCase> testCases)
        {
            ITestCaseFilterExpression filterExpression = GetFilterExpression();
            return filterExpression == null ? testCases : testCases.Where(testCase => Matches(testCase, filterExpression));
        }

        public bool Matches(TestCase testCase)
        {
            ITestCaseFilterExpression filterExpression = GetFilterExpression();
            return filterExpression == null || Matches(testCase, filterExpression);
        }


        private void InitProperties(ISet<string> traitNames)
        {
            _testPropertiesMap[nameof(TestCaseProperties.FullyQualifiedName)] = TestCaseProperties.FullyQualifiedName;
            _testPropertiesMap[nameof(TestCaseProperties.DisplayName)] = TestCaseProperties.DisplayName;
            _testPropertiesMap[nameof(TestCaseProperties.LineNumber)] = TestCaseProperties.LineNumber;
            _testPropertiesMap[nameof(TestCaseProperties.CodeFilePath)] = TestCaseProperties.CodeFilePath;
            _testPropertiesMap[nameof(TestCaseProperties.ExecutorUri)] = TestCaseProperties.ExecutorUri;
            _testPropertiesMap[nameof(TestCaseProperties.Id)] = TestCaseProperties.Id;
            _testPropertiesMap[nameof(TestCaseProperties.Source)] = TestCaseProperties.Source;

            foreach (string traitName in traitNames)
            {
                if (_testPropertiesMap.Keys.Contains(traitName))
                {
                    _logger.LogWarning(String.Format(Resources.TraitIgnoreMessage, traitName));
                    continue;
                }

                var traitTestProperty = TestProperty.Find(traitName) ??
                      TestProperty.Register(traitName, traitName, "", "", typeof(string), 
                        ValidateTraitValue, TestPropertyAttributes.None, typeof(TestCase));
                _traitPropertiesMap[traitName] = traitTestProperty;
            }
        }

        private TestProperty PropertyProvider(string propertyName)
        {
            TestProperty testProperty;

            _testPropertiesMap.TryGetValue(propertyName, out testProperty);

            if (testProperty == null)
                _traitPropertiesMap.TryGetValue(propertyName, out testProperty);

            return testProperty;
        }

        private object PropertyValueProvider(TestCase currentTest, string propertyName)
        {
            TestProperty testProperty = PropertyProvider(propertyName);
            if (testProperty == null)
                return null;

            if (currentTest.Properties.Contains(testProperty))
                return currentTest.GetPropertyValue(testProperty);

            if (_traitPropertyNames.Contains(propertyName))
                return GetTraitValues(currentTest, propertyName);

            return null;
        }

        private ITestCaseFilterExpression GetFilterExpression()
        {
            try
            {
                ITestCaseFilterExpression filterExpression = _runContext.GetTestCaseFilter(_allPropertyNames, PropertyProvider);

                string message = filterExpression == null
                        ? Resources.NoTestCaseFilter
                        : String.Format(Resources.TestCaseFilter, filterExpression.TestCaseFilterValue);
                _logger.DebugInfo(message);

                return filterExpression;
            }
            catch (TestPlatformFormatException e)
            {
                _logger.LogError(String.Format(Resources.FilterInvalid, e.Message));
                return null;
            }
        }

        private object GetTraitValues(TestCase testCase, string traitName)
        {
            IList<string> traitValues = testCase.Traits
                .Where(t => t.Name == traitName)
                .Select(t => t.Value)
                .ToList();

            if (traitValues.Count > 1)
                return traitValues;

            return traitValues.SingleOrDefault();
        }

        private bool Matches(TestCase testCase, ITestCaseFilterExpression filterExpression)
        {
            bool matches =
                filterExpression.MatchTestCase(testCase, propertyName => PropertyValueProvider(testCase, propertyName));

            string message = matches
                ? String.Format(Resources.Matches, testCase.DisplayName, filterExpression.TestCaseFilterValue)
                : String.Format(Resources.DoesntMatch, testCase.DisplayName, filterExpression.TestCaseFilterValue);
            _logger.DebugInfo(message);

            return matches;
        }

        private bool ValidateTraitValue(object value)
        {
            string traitValue = value as string;
            if (traitValue == null)
                return false;

            return TraitValueRegex.IsMatch(traitValue);
        }

    }

}