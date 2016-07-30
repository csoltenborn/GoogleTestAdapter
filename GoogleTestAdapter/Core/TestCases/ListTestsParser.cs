using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    public class ListTestsParser
    {
        private static readonly Regex SuiteRegex = new Regex($@"([\w\/]*)(?:{Regex.Escape(GoogleTestConstants.TypedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex NameRegex = new Regex($@"([\w\/]*)(?:{Regex.Escape(GoogleTestConstants.ParameterizedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex IsParamRegex = new Regex(@"(\w+/)?\w+/\d+", RegexOptions.Compiled);

        private readonly string _testNameSeparator;

        public ListTestsParser(TestEnvironment testEnvironment)
        {
            _testNameSeparator = testEnvironment.Options.TestNameSeparator;
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
            Match suiteMatch = SuiteRegex.Match(suiteLine);
            string suite = suiteMatch.Groups[1].Value;
            string typeParam = suiteMatch.Groups[2].Value
                .Replace("class ", "")
                .Replace("struct ", "");

            Match nameMatch = NameRegex.Match(testCaseLine);
            string name = nameMatch.Groups[1].Value;
            string param = nameMatch.Groups[2].Value;

            string fullyQualifiedName = $"{suite}.{name}";

            string displayName = GetDisplayName(fullyQualifiedName, typeParam, param);
            if (!string.IsNullOrEmpty(_testNameSeparator))
                displayName = displayName.Replace("/", _testNameSeparator);

            TestCaseDescriptor.TestTypes testType = TestCaseDescriptor.TestTypes.Simple;
            if (IsParamRegex.IsMatch(suite))
                testType = TestCaseDescriptor.TestTypes.TypeParameterized;
            else if (IsParamRegex.IsMatch(name))
                testType = TestCaseDescriptor.TestTypes.Parameterized;

            return new TestCaseDescriptor(suite, name, fullyQualifiedName, displayName, testType);
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