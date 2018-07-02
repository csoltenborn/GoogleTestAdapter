using System.Collections.Generic;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    public class TestCaseLocation : SourceFileLocation
    {
        public List<Trait> Traits { get; } = new List<Trait>();

        public TestCaseLocation(string symbol, string sourceFile, uint line) : base(symbol, sourceFile, line)
        {
        }
    }

}