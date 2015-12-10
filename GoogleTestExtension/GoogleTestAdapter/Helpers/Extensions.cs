using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Helpers
{

    public static class AllKindsOfExtensions
    {

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        internal static TestCase2 FindTestcase(this IEnumerable<TestCase2> testcases, string qualifiedName)
        {
            return testcases.FirstOrDefault(testcase => testcase.FullyQualifiedName.Split(' ')[0] == qualifiedName);
        }

        internal static IDictionary<string, List<TestCase2>> GroupByExecutable(this IEnumerable<TestCase2> testcases)
        {
            Dictionary<string, List<TestCase2>> groupedTestCases = new Dictionary<string, List<TestCase2>>();
            foreach (TestCase2 testCase in testcases)
            {
                List<TestCase2> group;
                if (groupedTestCases.ContainsKey(testCase.Source))
                {
                    group = groupedTestCases[testCase.Source];
                }
                else
                {
                    group = new List<TestCase2>();
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