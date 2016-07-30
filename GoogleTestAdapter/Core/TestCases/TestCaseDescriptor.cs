namespace GoogleTestAdapter.TestCases
{
    public class TestCaseDescriptor
    {
        public enum TestTypes { Simple, Parameterized, TypeParameterized }

        public string Suite { get; }
        public string Name { get; }

        public string FullyQualifiedName { get; }
        public string DisplayName { get; }
        public TestTypes TestType { get; }

        internal TestCaseDescriptor(string suite, string name, string fullyQualifiedName, string displayName, TestTypes testType)
        {
            Suite = suite;
            Name = name;
            DisplayName = displayName;
            FullyQualifiedName = fullyQualifiedName;
            TestType = testType;
        }

        public override string ToString()
        {
            return DisplayName;
        }

    }

}