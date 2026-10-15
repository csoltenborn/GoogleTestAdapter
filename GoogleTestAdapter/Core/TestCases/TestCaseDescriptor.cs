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

        /// <summary>
        /// Class and test group the test is shown in by Test Explorer, see <see cref="Model.TestCase.TestClass"/>
        /// </summary>
        public string TestClass { get; }
        public string TestGroup { get; }

        public TestCaseDescriptor(string suite, string name, string fullyQualifiedName, string displayName, TestTypes testType,
            string testClass = null, string testGroup = null)
        {
            Suite = suite;
            Name = name;
            DisplayName = displayName;
            FullyQualifiedName = fullyQualifiedName;
            TestType = testType;
            TestClass = testClass ?? suite;
            TestGroup = testGroup ?? name;
        }

        public override string ToString()
        {
            return DisplayName;
        }

    }

}