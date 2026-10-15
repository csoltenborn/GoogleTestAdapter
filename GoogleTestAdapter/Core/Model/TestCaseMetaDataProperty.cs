using System;
using System.Linq;

namespace GoogleTestAdapter.Model
{
    public class TestCaseMetaDataProperty : TestProperty
    {
        public static readonly string Id = $"{typeof(TestCaseMetaDataProperty).FullName}";
        public const string Label = "Test case meta data";

        private const string TearDownTestSuiteMarker = "TearDownTestSuite";

        /// <summary>
        /// The number of test cases in the test case's suite and executable, not counting TearDownTestSuite test cases
        /// </summary>
        public int NrOfTestCasesInSuite { get; }
        public int NrOfTestCasesInExecutable { get; }

        /// <summary>
        /// Whether the test case reports the result of the suite's TearDownTestSuite() (see option
        /// TearDownTestSuiteTestCases) rather than being a test of the executable
        /// </summary>
        public bool IsTearDownTestSuite { get; }

        public TestCaseMetaDataProperty(int nrOfTestCasesInSuite, int nrOfTestCasesInExecutable, bool isTearDownTestSuite = false)
            : this($"{nrOfTestCasesInSuite}:{nrOfTestCasesInExecutable}" + (isTearDownTestSuite ? $":{TearDownTestSuiteMarker}" : ""))
        {
        }

        public TestCaseMetaDataProperty(string serialization) : base(serialization)
        {
            string[] values = serialization.Split(':');
            if (values.Length != 2 && !(values.Length == 3 && values[2] == TearDownTestSuiteMarker))
                throw new ArgumentException(serialization, nameof(serialization));
            int[] numbers = values.Take(2).Select(int.Parse).ToArray();
            NrOfTestCasesInSuite = numbers[0];
            NrOfTestCasesInExecutable = numbers[1];
            IsTearDownTestSuite = values.Length == 3;
        }
    }
}
