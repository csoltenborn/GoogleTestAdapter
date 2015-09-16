
namespace GoogleTestAdapter.Helpers
{

    public class AbstractGoogleTestAdapterClass
    {
        protected AbstractOptions Options { get; private set; } = new GoogleTestAdapterOptions();

        protected AbstractGoogleTestAdapterClass(AbstractOptions options)
        {
            if (options != null)
            {
                this.Options = options;
            }
        }

    }

}
