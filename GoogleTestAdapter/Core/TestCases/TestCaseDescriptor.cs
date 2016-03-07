namespace GoogleTestAdapter.TestCases
{
    internal class TestCaseDescriptor
    {
        internal string Suite { get; }
        internal string Name { get; }
        internal string Param { get; }
        internal string TypeParam { get; }

        internal string FullyQualifiedName { get; }
        internal string DisplayName { get; }

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