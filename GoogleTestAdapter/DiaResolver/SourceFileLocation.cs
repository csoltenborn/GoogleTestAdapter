using System;

namespace GoogleTestAdapter.DiaResolver
{

    public class SourceFileLocation
    {
        private static readonly string Separator = "::";
        private static readonly int SeparatorLength = Separator.Length;

        public string Symbol { get; }
        public string Sourcefile { get; }
        public uint Line { get; }
        public string TestClassSignature { get; }
        public int IndexOfSerializedTrait { get; }

        public SourceFileLocation(string symbol, string sourceFile, uint line)
        {
            Symbol = symbol;
            Sourcefile = sourceFile;
            Line = line;
            IndexOfSerializedTrait = Symbol.LastIndexOf(Separator, StringComparison.Ordinal) + SeparatorLength;
            TestClassSignature = IndexOfSerializedTrait < SeparatorLength ? null : Symbol.Substring(0, IndexOfSerializedTrait - SeparatorLength);
        }

        public override string ToString()
        {
            return Symbol;
        }
    }

}