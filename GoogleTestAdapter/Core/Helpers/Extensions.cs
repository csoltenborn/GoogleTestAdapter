﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Helpers
{

    public static class AllKindsOfExtensions
    {

        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }

        public static bool MatchesCompletely(this Regex regex, string input)
        {
            if (regex == null)
                throw new ArgumentNullException(nameof(regex));
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            Match match = regex.Match(input);
            return match.Success && match.Length == input.Length;
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

    }

}