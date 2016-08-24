using System.Collections.Generic;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    public class ListTestsParser
    {
        private readonly string _testNameSeparator;

        public ListTestsParser(TestEnvironment testEnvironment)
        {
            _testNameSeparator = testEnvironment.Options.TestNameSeparator;
        }

        public IList<TestCaseDescriptor> ParseListTestsOutput(IEnumerable<string> consoleOutput)
        {
            var testCaseDescriptors = new List<TestCaseDescriptor>();
            var actualParser = new StreamingListTestsParser(_testNameSeparator, tcd => testCaseDescriptors.Add(tcd));
            foreach (string line in consoleOutput)
            {
                actualParser.ReportLine(line);
            }
            return testCaseDescriptors;
        }

    }

}