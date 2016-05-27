namespace GoogleTestAdapter.DiaResolver
{

    public class SourceFileLocation
    {
        public string Symbol { get; }
        public string Sourcefile { get; }
        public uint Line { get; }

        public SourceFileLocation(string symbol, string sourceFile, uint line)
        {
            Symbol = symbol;
            Sourcefile = sourceFile;
            Line = line;
        }

        public override string ToString()
        {
            return Symbol;
        }
    }

}