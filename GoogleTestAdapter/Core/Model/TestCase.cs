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

        /// <summary>
        /// C++ namespace the test is defined in, e.g. "outer::inner" or "(anonymous namespace)"; empty for the
        /// global namespace, null if unknown (i.e., if the test's symbol could not be resolved)
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Class and test group Test Explorer shows the test in: the test suite and the test's name as written in the
        /// code, i.e. instances of typed and parameterized tests share them (e.g. "Suite" and "Test" for
        /// "Prefix/Suite.Test/0"); null if unknown, in which case they are derived from the fully qualified name
        /// </summary>
        public string TestClass { get; set; }
        public string TestGroup { get; set; }

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

        /// <summary>
        /// Whether the test case reports the result of TearDownTestSuite() of its suite (see option
        /// TearDownTestSuiteTestCases)
        /// </summary>
        public bool IsTearDownTestSuiteTestCase
        {
            get { return Properties.OfType<TestCaseMetaDataProperty>().Any(p => p.IsTearDownTestSuite); }
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