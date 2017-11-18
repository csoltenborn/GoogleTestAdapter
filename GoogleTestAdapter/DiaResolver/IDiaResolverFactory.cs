using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{
    public interface IDiaResolverFactory
    {
        IDiaResolver Create(string binary, string pdb, ILogger logger);
    }
}