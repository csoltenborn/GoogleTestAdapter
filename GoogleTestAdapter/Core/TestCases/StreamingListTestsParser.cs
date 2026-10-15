using System;
using System.Text.RegularExpressions;

namespace GoogleTestAdapter.TestCases
{

    public class StreamingListTestsParser
    {
        // Suites and tests are named by C++ identifiers (and "/" for instantiations of typed and parameterized tests).
        // Identifiers may contain characters \w does not match (e.g. spacing combining marks \p{Mc} as in Devanagari,
        // and letter numbers \p{Nl}), so all characters but whitespace and the separators are accepted.
        private const string NameChar = @"[^\s./]";
        private static readonly Regex SuiteRegex = new Regex($@"((?:{NameChar}|/)*(?:\.(?:{NameChar}|/)+)*)(?:{Regex.Escape(GoogleTestConstants.TypedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex NameRegex = new Regex($@"((?:{NameChar}|/)*)(?:{Regex.Escape(GoogleTestConstants.ParameterizedTestMarker)}(.*))?", RegexOptions.Compiled);
        private static readonly Regex IsParamRegex = new Regex($@"({NameChar}+/)?{NameChar}+/{NameChar}+", RegexOptions.Compiled);
        private static readonly Regex IsParamRegexPreNamedParameters = new Regex($@"({NameChar}+/)?{NameChar}+/\d+", RegexOptions.Compiled);
        private static readonly Regex StructKeywordsRegex = new Regex(@"\b(?:class|struct) ", RegexOptions.Compiled);

        private readonly string _testNameSeparator;

        private string _currentSuite = "";

        public StreamingListTestsParser(string testNameSeparator)
        {
            _testNameSeparator = testNameSeparator;
        }

        public class TestCaseDescriptorCreatedEventArgs : EventArgs
        {
            public TestCaseDescriptor TestCaseDescriptor { get; set; }
        }

        public event EventHandler<TestCaseDescriptorCreatedEventArgs> TestCaseDescriptorCreated;


        public void ReportLine(string line)
        {
            string trimmedLine = line.Trim('.', '\n', '\r');
            if (trimmedLine.StartsWith("  "))
            {
                TestCaseDescriptor descriptor = CreateDescriptor(_currentSuite, trimmedLine.Substring(2));
                TestCaseDescriptorCreated?.Invoke(this,
                    new TestCaseDescriptorCreatedEventArgs {TestCaseDescriptor = descriptor});
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
            string typeParam = StructKeywordsRegex.Replace(suiteMatch.Groups[2].Value, "");

            Match nameMatch = NameRegex.Match(testCaseLine);
            string name = nameMatch.Groups[1].Value;
            string param = nameMatch.Groups[2].Value;

            string fullyQualifiedName = $"{suite}.{name}";

            string displayName = GetDisplayName(fullyQualifiedName, typeParam, param);
            if (!string.IsNullOrEmpty(_testNameSeparator))
                displayName = displayName.Replace("/", _testNameSeparator);

            TestCaseDescriptor.TestTypes testType = TestCaseDescriptor.TestTypes.Simple;
            if (string.IsNullOrWhiteSpace(typeParam) ? IsParamRegexPreNamedParameters.IsMatch(suite): IsParamRegex.IsMatch(suite))
                testType = TestCaseDescriptor.TestTypes.TypeParameterized;
            else if (string.IsNullOrWhiteSpace(param) ? IsParamRegexPreNamedParameters.IsMatch(name) : IsParamRegex.IsMatch(name))
                testType = TestCaseDescriptor.TestTypes.Parameterized;

            // older versions of Google Test do not always print the type and value parameters
            bool isTyped = !string.IsNullOrEmpty(typeParam) || testType == TestCaseDescriptor.TestTypes.TypeParameterized;
            bool isValueParameterized = !string.IsNullOrEmpty(param) || testType == TestCaseDescriptor.TestTypes.Parameterized;
            return new TestCaseDescriptor(suite, name, fullyQualifiedName, displayName, testType,
                GetTestClass(suite, isTyped, isValueParameterized),
                GetTestGroup(name, isValueParameterized));
        }

        // Instances of typed and parameterized tests are grouped by the suite as written in the code, i.e. without
        // instantiation prefix and type index: "Prefix/Suite/0" (type-parameterized), "Suite/0" (typed) and
        // "Prefix/Suite" (value-parameterized) => "Suite" (upstream #210)
        public static string GetTestClass(string suite, bool isTyped, bool isValueParameterized)
        {
            string testClass = suite;
            if (isTyped)
            {
                int indexOfTypeIndex = testClass.LastIndexOf('/');
                if (indexOfTypeIndex > 0)
                    testClass = testClass.Substring(0, indexOfTypeIndex);
            }
            if (isTyped || isValueParameterized)
            {
                testClass = testClass.Substring(testClass.LastIndexOf('/') + 1);
            }
            return testClass;
        }

        // "Test/0" and "Test/MyName" (value-parameterized) => "Test"
        public static string GetTestGroup(string name, bool isValueParameterized)
        {
            int indexOfParamIndex = name.IndexOf('/');
            return isValueParameterized && indexOfParamIndex > 0 ? name.Substring(0, indexOfParamIndex) : name;
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