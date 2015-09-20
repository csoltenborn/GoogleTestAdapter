namespace GoogleTestAdapter.Helpers
{

    public class AbstractOptionsProvider
    {

        protected AbstractOptions Options { get; }

        protected AbstractOptionsProvider(AbstractOptions options)
        {
            this.Options = options ?? new GoogleTestAdapterOptions();
        }

    }

}