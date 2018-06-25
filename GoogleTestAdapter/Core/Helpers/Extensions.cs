// This file has been modified by Microsoft on 7/2017.

using System.Collections.Generic;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Helpers
{

    public static class AllKindsOfExtensions
    {

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        internal static IDictionary<string, List<TestCase>> GroupByExecutable(this IEnumerable<TestCase> testcases)
        {
            var groupedTestCases = new Dictionary<string, List<TestCase>>();
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

        public static void AddRange<T1, T2>(this IDictionary<T1, T2> target, IDictionary<T1, T2> source, bool replaceExisting = false)
        {
            foreach (KeyValuePair<T1, T2> keyValuePair in source)
            {
                if (target.ContainsKey(keyValuePair.Key))
                {
                    if (replaceExisting)
                    {
                        target.Remove(keyValuePair.Key);
                        target.Add(keyValuePair);
                    }
                }
                else
                {
                    target.Add(keyValuePair);
                }
            }
        }

    }

}