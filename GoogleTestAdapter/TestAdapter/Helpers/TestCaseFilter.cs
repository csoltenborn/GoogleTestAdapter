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
        private IRunContext RunContext { get; }
        private TestEnvironment TestEnvironment { get; }

        private IDictionary<string, TestProperty> TestPropertiesMap { get; } = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
        private IDictionary<string, TestProperty> TraitPropertiesMap { get; } = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);

        private IEnumerable<string> TestProperties => TestPropertiesMap.Keys;
        private IEnumerable<string> TraitProperties => TraitPropertiesMap.Keys;
        private IEnumerable<string> AllProperties => TestProperties.Union(TraitProperties);

        public TestCaseFilter(IRunContext runContext, ISet<string> traitNames, TestEnvironment testEnvironment)
        {
            this.RunContext = runContext;
            this.TestEnvironment = testEnvironment;

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
            TestPropertiesMap[nameof(TestCaseProperties.FullyQualifiedName)] = TestCaseProperties.FullyQualifiedName;
            TestPropertiesMap[nameof(TestCaseProperties.DisplayName)] = TestCaseProperties.DisplayName;
            TestPropertiesMap[nameof(TestCaseProperties.LineNumber)] = TestCaseProperties.LineNumber;
            TestPropertiesMap[nameof(TestCaseProperties.CodeFilePath)] = TestCaseProperties.CodeFilePath;
            TestPropertiesMap[nameof(TestCaseProperties.ExecutorUri)] = TestCaseProperties.ExecutorUri;
            TestPropertiesMap[nameof(TestCaseProperties.Id)] = TestCaseProperties.Id;
            TestPropertiesMap[nameof(TestCaseProperties.Source)] = TestCaseProperties.Source;

            foreach (string traitName in traitNames)
            {
                var traitTestProperty = TestProperty.Find(traitName) ??
                      TestProperty.Register(traitName, traitName, typeof(string), typeof(TestCase));
                TraitPropertiesMap[traitName] = traitTestProperty;
            }
        }

        private TestProperty PropertyProvider(string propertyName)
        {
            TestProperty testProperty;

            TestPropertiesMap.TryGetValue(propertyName, out testProperty);

            if (testProperty == null)
                TraitPropertiesMap.TryGetValue(propertyName, out testProperty);

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
            ITestCaseFilterExpression filterExpression = RunContext.GetTestCaseFilter(AllProperties, PropertyProvider);

            string message = filterExpression == null
                    ? "No test case filter provided"
                    : $"Test case filter: {filterExpression.TestCaseFilterValue}";
            TestEnvironment.DebugInfo(message);

            return filterExpression;
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
            TestEnvironment.DebugInfo(message);

            return matches;
        }

    }

}