using System.Collections.Generic;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    public class TestCaseLocation : SourceFileLocation
    {
        public List<Trait> Traits { get; } = new List<Trait>();

        /// <summary>
        /// C++ namespace of the test, see <see cref="TestCase.Namespace"/>
        /// </summary>
        public string Namespace { get; set; }

        public TestCaseLocation(string symbol, string sourceFile, uint line) : base(symbol, sourceFile, line)
        {
        }
    }

}