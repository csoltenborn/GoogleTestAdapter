using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    public class ListTestsParser
    {
        private TestEnvironment TestEnvironment { get; }
        private string TestNameSeparator { get; }

        public ListTestsParser(TestEnvironment testEnvironment)
        {
            TestEnvironment = testEnvironment;
            TestNameSeparator = TestEnvironment.Options.TestNameSeparator;
        }

        public IList<TestCaseDescriptor> ParseListTestsOutput(List<string> consoleOutput)
        {
            var testCaseDescriptors = new List<TestCaseDescriptor>();
            string currentSuite = "";
            foreach (string trimmedLine in consoleOutput.Select(line => line.Trim('.', '\n', '\r')))
            {
                if (trimmedLine.StartsWith("  "))
                {
                    testCaseDescriptors.Add(
                        CreateDescriptor(currentSuite, trimmedLine.Substring(2)));
                }
                else
                {
                    currentSuite = trimmedLine;
                }
            }

            return testCaseDescriptors;
        }

        private TestCaseDescriptor CreateDescriptor(string suiteLine, string testCaseLine)
        {
            string[] split = suiteLine.Split(new[] { GoogleTestConstants.TypedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
            string suite = split.Length > 0 ? split[0] : suiteLine;
            string typeParam = null;
            if (split.Length > 1)
            {
                typeParam = split[1];
                typeParam = typeParam.Replace("class ", "");
                typeParam = typeParam.Replace("struct ", "");
            }

            split = testCaseLine.Split(new[] { GoogleTestConstants.ParameterizedTestMarker }, StringSplitOptions.RemoveEmptyEntries);
            string name = split.Length > 0 ? split[0] : testCaseLine;
            string param = null;
            if (split.Length > 1)
            {
                param = split[1];
            }

            string fullyQualifiedName = $"{suite}.{name}";

            string displayName = GetDisplayName(fullyQualifiedName, typeParam, param);
            if (!string.IsNullOrEmpty(TestNameSeparator))
                displayName = displayName.Replace("/", TestNameSeparator);

            return new TestCaseDescriptor(suite, name, typeParam, param, fullyQualifiedName, displayName);
        }

        private static string GetDisplayName(string fullyQalifiedName, string typeParam, string param)
        {
            string displayName = fullyQalifiedName;
            if (!string.IsNullOrEmpty(typeParam))
            {
                displayName += GetEnclosedTypeParam(typeParam);
            }
            if (!string.IsNullOrEmpty(param))
            {
                displayName += $" [{param}]";
            }

            return displayName;
        }

        internal static string GetEnclosedTypeParam(string typeParam)
        {
            if (typeParam.EndsWith(">"))
            {
                typeParam += " ";
            }
            return $"<{typeParam}>";
        }

    }

}