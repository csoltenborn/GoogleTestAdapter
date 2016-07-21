using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{
    public class DefaultDiaResolverFactory : IDiaResolverFactory
    {
        public static IDiaResolverFactory Instance { get; } = new DefaultDiaResolverFactory();

        public IDiaResolver Create(string binary, string pathExtensions, ILogger logger, bool debugMode)
        {
            return new DiaResolver(binary, pathExtensions, logger, debugMode);
        }
    }
}