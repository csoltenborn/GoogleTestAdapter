namespace GoogleTestAdapter.Helpers
{

    public class AbstractGoogleTestAdapterClass
    {

        protected AbstractOptions Options { get; }

        protected AbstractGoogleTestAdapterClass(AbstractOptions options)
        {
            this.Options = options ?? new GoogleTestAdapterOptions();
        }

    }

}