namespace DiaAdapter
{

    /*
    Symbol=[<namespace>::]<test_case_name>_<test_name>_Test::TestBody
    */
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