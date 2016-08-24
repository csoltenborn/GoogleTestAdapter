using System;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter.TestCases
{

    public class StreamingListTestsParser
    {
        private static readonly Regex SuiteRegex = new Regex($@"([\w\/]*)(?:{Regex.Escape(GoogleTestConstants.TypedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex NameRegex = new Regex($@"([\w\/]*)(?:{Regex.Escape(GoogleTestConstants.ParameterizedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex IsParamRegex = new Regex(@"(\w+/)?\w+/\d+", RegexOptions.Compiled);

        private readonly string _testNameSeparator;
        private readonly Action<TestCaseDescriptor> _onDescriptorCreated;

        private string _currentSuite = "";

        public StreamingListTestsParser(string testNameSeparator, Action<TestCaseDescriptor> onDescriptorCreated)
        {
            _onDescriptorCreated = onDescriptorCreated;
            _testNameSeparator = testNameSeparator;
        }

        public void ReportLine(string line)
        {
            string trimmedLine = line.Trim('.', '\n', '\r');
            if (trimmedLine.StartsWith("  "))
            {
                _onDescriptorCreated(
                    CreateDescriptor(_currentSuite, trimmedLine.Substring(2)));
            }
            else
            {
                _currentSuite = trimmedLine;
            }
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

        private static string GetEnclosedTypeParam(string typeParam)
        {
            if (typeParam.EndsWith(">"))
            {
                typeParam += " ";
            }
            return $"<{typeParam}>";
        }

    }

}