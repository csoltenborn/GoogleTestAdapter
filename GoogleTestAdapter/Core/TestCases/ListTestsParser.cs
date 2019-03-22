using System.Collections.Generic;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    public class ListTestsParser
    {
        private readonly string _testNameSeparator;

        public ListTestsParser(string testNameSeparator)
        {
            _testNameSeparator = testNameSeparator;
        }

        public IList<TestCase> ParseListTestsOutput(IEnumerable<string> consoleOutput)
        {
            var testCases = new List<TestCase>();

            var actualParser = new StreamingListTestsParser(_testNameSeparator);
            actualParser.TestCaseCreated += (sender, args) => testCases.Add(args.TestCase);

            foreach (string line in consoleOutput)
            {
                actualParser.ReportLine(line);
            }
            return testCases;
        }

    }

}