using System;
using System.Linq;

namespace GoogleTestAdapter.Model
{
    public class TestCaseMetaDataProperty : TestProperty
    {
        public static readonly string Id = $"{typeof(TestCaseMetaDataProperty).FullName}";
        public const string Label = "Test case meta data";

        public int NrOfTestCasesInSuite { get; }
        public int NrOfTestCasesInExecutable { get; }

        public TestCaseMetaDataProperty(int nrOfTestCasesInSuite, int nrOfTestCasesInExecutable)
            : this($"{nrOfTestCasesInSuite}:{nrOfTestCasesInExecutable}")
        {
        }

        public TestCaseMetaDataProperty(string serialization) : base(serialization)
        {
            int[] values = serialization.Split(':').Select(int.Parse).ToArray();
            if (values.Length != 2)
                throw new ArgumentException(serialization, nameof(serialization));
            NrOfTestCasesInSuite = values[0];
            NrOfTestCasesInExecutable = values[1];
        }
    }
}