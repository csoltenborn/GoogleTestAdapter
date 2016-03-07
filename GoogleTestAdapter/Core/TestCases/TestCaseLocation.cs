using System.Collections.Generic;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseLocation : SourceFileLocation
    {
        internal List<Trait> Traits { get; } = new List<Trait>();

        internal TestCaseLocation(string symbol, string sourceFile, uint line) : base(symbol, sourceFile, line)
        {
        }
    }

}