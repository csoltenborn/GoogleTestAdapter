using System.Collections.Generic;

namespace GoogleTestAdapter.Model
{
    public class TestCase
    {
        public string Source { get; }

        public string FullyQualifiedName { get; }
        public string DisplayName { get; }

        public string CodeFilePath { get; }
        public int LineNumber { get; }

        public List<Trait> Traits { get; } = new List<Trait>();

        public TestCase(string fullyQualifiedName, string source, string displayName, string codeFilePath, int lineNumber)
        {
            FullyQualifiedName = fullyQualifiedName;
            Source = source;
            DisplayName = displayName;
            CodeFilePath = codeFilePath;
            LineNumber = lineNumber;
        }

        public override bool Equals(object obj)
        {
            var other = obj as TestCase;

            if (other == null)
                return false;

            return FullyQualifiedName == other.FullyQualifiedName && Source == other.Source;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + FullyQualifiedName.GetHashCode();
            hash = hash * 31 + Source.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        internal static IDictionary<string, List<TestCase>> GroupByExecutable(IEnumerable<TestCase> testcases)
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

    }

}