using GoogleTestAdapter.Model;
using System;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter.TestCases
{

    public class StreamingListTestsParser
    {
        private static readonly Regex SuiteRegex = new Regex($@"([\w\/]*(?:\.[\w\/]+)*)(?:{Regex.Escape(GoogleTestConstants.TypedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex NameRegex = new Regex($@"([\w\/]*)(?:{Regex.Escape(GoogleTestConstants.ParameterizedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex IsParamRegex = new Regex(@"(\w+/)?\w+/\w+", RegexOptions.Compiled);
        private static readonly Regex IsParamRegexPreNamedParameters = new Regex(@"(\w+/)?\w+/\d+", RegexOptions.Compiled);
        private static readonly Regex StructKeywordsRegex = new Regex(@"\b(?:class|struct) ", RegexOptions.Compiled);

        private readonly string _testNameSeparator;

        private string _currentSuite = "";

        public StreamingListTestsParser(string testNameSeparator)
        {
            _testNameSeparator = testNameSeparator;
        }

        public class TestCaseCreatedEventArgs : EventArgs
        {
            public TestCase TestCase { get; set; }
        }

        public event EventHandler<TestCaseCreatedEventArgs> TestCaseCreated;

        public static Regex matchLocationLine = new Regex("^ *<loc>(.*)\\((\\d+)\\)");

        /// <summary>
        /// Source code location / line position where particular test resides.
        /// </summary>
        String _currentSource = null;
        int _currentLineNumber;


        public void ReportLine(string _line)
        {
            string line = _line.Trim('.', '\n', '\r');

            var m = matchLocationLine.Match(line);
            if (m.Success)
            {
                _currentSource = m.Groups[1].Value;
                if (!Int32.TryParse(m.Groups[2].Value, out _currentLineNumber))
                    _currentLineNumber = 1;

                return;
            }
            
            if (line.StartsWith("  ", StringComparison.Ordinal))
            {
                TestCase testcase = CreateTestCase(_currentSuite, line.Substring(2));
                if (_currentSource != null)
                {
                    testcase.CodeFilePath = _currentSource;
                    testcase.LineNumber = _currentLineNumber;
                }

                TestCaseCreated?.Invoke(this, new TestCaseCreatedEventArgs {TestCase = testcase});
                _currentSource = null;
                _currentLineNumber = 1;
            }
            else
            {
                _currentSuite = line;
            }
        }

        private TestCase CreateTestCase(string suiteLine, string testCaseLine)
        {
            Match suiteMatch = SuiteRegex.Match(suiteLine);
            string suite = suiteMatch.Groups[1].Value;
            string typeParam = StructKeywordsRegex.Replace(suiteMatch.Groups[2].Value, "");

            Match nameMatch = NameRegex.Match(testCaseLine);
            string name = nameMatch.Groups[1].Value;
            string param = nameMatch.Groups[2].Value;

            string fullyQualifiedName = $"{suite}.{name}";

            string displayName = GetDisplayName(fullyQualifiedName, typeParam, param);
            if (!string.IsNullOrEmpty(_testNameSeparator))
                displayName = displayName.Replace("/", _testNameSeparator);

            TestCase.TestTypes testType = TestCase.TestTypes.Simple;
            if (string.IsNullOrWhiteSpace(typeParam) ? IsParamRegexPreNamedParameters.IsMatch(suite): IsParamRegex.IsMatch(suite))
                testType = TestCase.TestTypes.TypeParameterized;
            else if (string.IsNullOrWhiteSpace(param) ? IsParamRegexPreNamedParameters.IsMatch(name) : IsParamRegex.IsMatch(name))
                testType = TestCase.TestTypes.Parameterized;

            return new TestCase(suite, fullyQualifiedName, null, name, displayName) { TestType = testType };
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