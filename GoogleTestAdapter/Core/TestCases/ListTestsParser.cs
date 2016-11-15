using System.Collections.Generic;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.TestCases
{

    public class ListTestsParser
    {
        private readonly string _testNameSeparator;

        public ListTestsParser(string testNameSeparator)
        {
            _testNameSeparator = testNameSeparator;
        }

        public IList<TestCaseDescriptor> ParseListTestsOutput(IEnumerable<string> consoleOutput)
        {
            var testCaseDescriptors = new List<TestCaseDescriptor>();

            var actualParser = new StreamingListTestsParser(_testNameSeparator);
            actualParser.TestCaseDescriptorCreated += (sender, args) => testCaseDescriptors.Add(args.TestCaseDescriptor);

            foreach (string line in consoleOutput)
            {
                actualParser.ReportLine(line);
            }
            return testCaseDescriptors;
        }

    }

}