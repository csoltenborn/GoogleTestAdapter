using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.DiaResolver
{
    public class DefaultDiaResolverFactory : IDiaResolverFactory
    {
        public static IDiaResolverFactory Instance { get; } = new DefaultDiaResolverFactory();

        public IDiaResolver Create(string binary, string pdb, ILogger logger)
        {
            return new DiaResolver(binary, pdb, logger);
        }
    }
}