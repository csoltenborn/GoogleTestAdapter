using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Helpers
{

    static class AllKindsOfExtensions
    {

        internal static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        internal static TestCase FindTestcase(this IEnumerable<TestCase> testcases, string qualifiedName)
        {
            return testcases.FirstOrDefault(testcase => testcase.FullyQualifiedName.Split(' ')[0] == qualifiedName);
        }

        internal static IDictionary<string, List<TestCase>> GroupByExecutable(this IEnumerable<TestCase> testcases)
        {
            Dictionary<string, List<TestCase>> groupedTestCases = new Dictionary<string, List<TestCase>>();
            foreach (TestCase testCase in testcases)
            {
                List<TestCase> group;
                if (groupedTestCases.ContainsKey(testCase.Source))
                {
                    group = groupedTestCases[testCase.Source];
                }
                else
                {
                    group = new List<TestCase>();
                    groupedTestCases.Add(testCase.Source, group);
                }
                group.Add(testCase);
            }
            return groupedTestCases;
        }

        internal static string AppendIfNotEmpty(this string theString, string appendix)
        {
            return string.IsNullOrWhiteSpace(theString) ? theString : theString + appendix;
        }

    }

}