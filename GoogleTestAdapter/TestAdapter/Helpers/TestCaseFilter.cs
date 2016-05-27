using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestAdapter.Helpers
{

    public class TestCaseFilter
    {
        private readonly IRunContext _runContext;
        private readonly TestEnvironment _testEnvironment;

        private readonly IDictionary<string, TestProperty> _testPropertiesMap = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, TestProperty> _traitPropertiesMap = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);

        private IEnumerable<string> TestProperties => _testPropertiesMap.Keys;
        private IEnumerable<string> TraitProperties => _traitPropertiesMap.Keys;
        private IEnumerable<string> AllProperties => TestProperties.Union(TraitProperties);

        public TestCaseFilter(IRunContext runContext, ISet<string> traitNames, TestEnvironment testEnvironment)
        {
            _runContext = runContext;
            _testEnvironment = testEnvironment;

            InitProperties(traitNames);
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
                var traitTestProperty = TestProperty.Find(traitName) ??
                      TestProperty.Register(traitName, traitName, typeof(string), typeof(TestCase));
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

            if (TraitProperties.Contains(propertyName))
                return GetTraitValues(currentTest, propertyName);

            return null;
        }

        private ITestCaseFilterExpression GetFilterExpression()
        {
            try
            {
                ITestCaseFilterExpression filterExpression = _runContext.GetTestCaseFilter(AllProperties, PropertyProvider);

                string message = filterExpression == null
                        ? "No test case filter provided"
                        : $"Test case filter: {filterExpression.TestCaseFilterValue}";
                _testEnvironment.DebugInfo(message);

                return filterExpression;
            }
            catch (TestPlatformFormatException e)
            {
                _testEnvironment.LogWarning(e.Message);
                return null;
            }
        }

        private object GetTraitValues(TestCase testCase, string traitName)
        {
            IList<string> traitValues
                = testCase.Traits.Where(t => t.Name == traitName).Select(t => t.Value).ToList();

            if (traitValues.Count > 1)
                return traitValues;

            return traitValues.SingleOrDefault();
        }

        private bool Matches(TestCase testCase, ITestCaseFilterExpression filterExpression)
        {
            bool matches =
                filterExpression.MatchTestCase(testCase, propertyName => PropertyValueProvider(testCase, propertyName));

            string message = matches
                ? $"{testCase.DisplayName} matches {filterExpression.TestCaseFilterValue}"
                : $"{testCase.DisplayName} does not match {filterExpression.TestCaseFilterValue}";
            _testEnvironment.DebugInfo(message);

            return matches;
        }

    }

}