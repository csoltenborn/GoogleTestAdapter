using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{
    public interface IDiaResolverFactory
    {
        IDiaResolver Create(string binary, string pathExtensions, ILogger logger, bool debugMode);
    }
}