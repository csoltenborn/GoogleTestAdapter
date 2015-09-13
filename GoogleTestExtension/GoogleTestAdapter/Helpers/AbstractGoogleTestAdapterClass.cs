
namespace GoogleTestAdapter
{

    public class AbstractGoogleTestAdapterClass
    {
        protected IOptions Options { get; private set; } = new GoogleTestAdapterOptions();

        protected AbstractGoogleTestAdapterClass(IOptions options)
        {
            if (options != null)
            {
                this.Options = options;
            }
        }

    }

}
