using System.Collections.Generic;
using System.Linq;

namespace GoogleTestAdapter.Model
{
    public class TestCase
    {
        public enum TestTypes { Simple, Parameterized, TypeParameterized }

        public string Suite { get; }
        public string Source;

        public string FullyQualifiedName { get; }
        public string Name;
        public string DisplayName { get; }

        public string CodeFilePath = string.Empty;
        public int LineNumber = 0;

        public TestTypes TestType;

        public List<Trait> Traits { get; } = new List<Trait>();
        public List<TestProperty> Properties { get; } = new List<TestProperty>();

        public TestCase(string fullyQualifiedName, string source, string displayName, string codeFilePath, int lineNumber)
        {
            FullyQualifiedName = fullyQualifiedName;
            Source = source;
            DisplayName = displayName;
            CodeFilePath = codeFilePath;
            LineNumber = lineNumber;
        }

        public bool IsExitCodeTestCase
        {
            get { return !Properties.Any(p => p is TestCaseMetaDataProperty); }
        }

        public TestCase(string suite, string fullyQualifiedName, string source, string name, string displayName)
        {
            Suite = suite;
            FullyQualifiedName = fullyQualifiedName;
            Source = source;
            Name = name;
            DisplayName = displayName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TestCase other))
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

    }

}