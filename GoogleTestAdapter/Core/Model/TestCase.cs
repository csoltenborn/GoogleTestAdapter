using System.Collections.Generic;
using System.Linq;

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