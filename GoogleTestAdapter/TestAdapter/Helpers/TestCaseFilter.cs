using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace GoogleTestAdapter.TestAdapter.Helpers
{

    internal class TestCaseFilter
    {

        private static readonly List<string> supportedProperties;
        private static readonly Dictionary<string, TestProperty> supportedPropertiesCache;

        static TestCaseFilter()
        {
            supportedPropertiesCache = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
            supportedPropertiesCache["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName;
            supportedPropertiesCache["Name"] = TestCaseProperties.DisplayName;
            supportedPropertiesCache["DisplayName"] = TestCaseProperties.DisplayName;
            supportedPropertiesCache["Line"] = TestCaseProperties.LineNumber;
            supportedPropertiesCache["LineNumber"] = TestCaseProperties.LineNumber;
            supportedPropertiesCache["CodeFilePath"] = TestCaseProperties.CodeFilePath;
            supportedPropertiesCache["SourceFile"] = TestCaseProperties.CodeFilePath;
            supportedPropertiesCache["ExecutorUri"] = TestCaseProperties.ExecutorUri;
            supportedPropertiesCache["Uri"] = TestCaseProperties.ExecutorUri;
            supportedPropertiesCache["Id"] = TestCaseProperties.Id;
            supportedPropertiesCache["Source"] = TestCaseProperties.Source;
            supportedPropertiesCache["TestExecutable"] = TestCaseProperties.Source;

            supportedProperties = new List<string>();
            supportedProperties.AddRange(supportedPropertiesCache.Keys);
        }

        private static TestProperty PropertyProvider(string propertyName)
        {
            TestProperty testProperty;
            supportedPropertiesCache.TryGetValue(propertyName, out testProperty);
            return testProperty;
        }

        private static object PropertyValueProvider(TestCase currentTest, string propertyName)
        {
            TestProperty testProperty = PropertyProvider(propertyName);
            if (testProperty != null && currentTest.Properties.Contains(testProperty))
            {
                return currentTest.GetPropertyValue(testProperty);
            }
            return null;
        }


        private readonly IRunContext runContext;

        internal TestCaseFilter(IRunContext runContext)
        {
            this.runContext = runContext;
        }

        public IEnumerable<TestCase> Filter(IEnumerable<TestCase> testCases)
        {
            ITestCaseFilterExpression filterExpression = runContext.GetTestCaseFilter(supportedProperties, PropertyProvider);
            if (filterExpression == null)
            {
                return testCases;
            }
            return testCases.Where(testCase => Matches(testCase, filterExpression));
        }

        public bool Matches(TestCase testCase)
        {
            return Matches(testCase, runContext.GetTestCaseFilter(supportedProperties, PropertyProvider));
        }

        private bool Matches(TestCase testCase, ITestCaseFilterExpression filterExpression)
        {
            if (filterExpression == null)
            {
                return true;
            }
            return filterExpression.MatchTestCase(testCase, propertyName => PropertyValueProvider(testCase, propertyName));
        }

    }

}