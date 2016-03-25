namespace GoogleTestAdapter.DiaResolver
{
    public interface IDiaResolverFactory
    {
        IDiaResolver Create(string binary, string pathExtensions);
    }
}