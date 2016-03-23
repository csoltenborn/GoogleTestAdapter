namespace GoogleTestAdapter.TestCases
{
    public class TestCaseDescriptor
    {
        public string Suite { get; }
        public string Name { get; }
        public string Param { get; }
        public string TypeParam { get; }

        public string FullyQualifiedName { get; }
        public string DisplayName { get; }

        internal TestCaseDescriptor(string suite, string name, string typeParam, string param, string fullyQualifiedName, string displayName)
        {
            Suite = suite;
            Name = name;
            TypeParam = typeParam;
            Param = param;
            DisplayName = displayName;
            FullyQualifiedName = fullyQualifiedName;
        }

        public override string ToString()
        {
            return DisplayName;
        }

    }

}