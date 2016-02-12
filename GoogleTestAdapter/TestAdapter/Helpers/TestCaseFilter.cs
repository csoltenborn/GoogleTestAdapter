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
        private readonly IRunContext runContext;
        private readonly TestEnvironment testEnvironment;

        private readonly IDictionary<string, TestProperty> testProperties = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, TestProperty> traitProperties = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);

        private IEnumerable<string> TestProperties => testProperties.Keys;
        private IEnumerable<string> TraitProperties => traitProperties.Keys;
        private IEnumerable<string> AllProperties => TestProperties.Union(TraitProperties);

        public TestCaseFilter(IRunContext runContext, ISet<string> traitNames, TestEnvironment testEnvironment)
        {
            this.runContext = runContext;
            this.testEnvironment = testEnvironment;

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
            testProperties[nameof(TestCaseProperties.FullyQualifiedName)] = TestCaseProperties.FullyQualifiedName;
            testProperties[nameof(TestCaseProperties.DisplayName)] = TestCaseProperties.DisplayName;
            testProperties[nameof(TestCaseProperties.LineNumber)] = TestCaseProperties.LineNumber;
            testProperties[nameof(TestCaseProperties.CodeFilePath)] = TestCaseProperties.CodeFilePath;
            testProperties[nameof(TestCaseProperties.ExecutorUri)] = TestCaseProperties.ExecutorUri;
            testProperties[nameof(TestCaseProperties.Id)] = TestCaseProperties.Id;
            testProperties[nameof(TestCaseProperties.Source)] = TestCaseProperties.Source;

            foreach (string traitName in traitNames)
            {
                var traitTestProperty = TestProperty.Find(traitName) ??
                      TestProperty.Register(traitName, traitName, typeof(string), typeof(TestCase));
                traitProperties[traitName] = traitTestProperty;
            }
        }

        private TestProperty PropertyProvider(string propertyName)
        {
            TestProperty testProperty;

            testProperties.TryGetValue(propertyName, out testProperty);

            if (testProperty == null)
                traitProperties.TryGetValue(propertyName, out testProperty);

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
            ITestCaseFilterExpression filterExpression = runContext.GetTestCaseFilter(AllProperties, PropertyProvider);

            string message = filterExpression == null
                    ? "No test case filter provided"
                    : $"Test case filter: {filterExpression.TestCaseFilterValue}";
            testEnvironment.DebugInfo(message);

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
            testEnvironment.DebugInfo(message);

            return matches;
        }

    }

}