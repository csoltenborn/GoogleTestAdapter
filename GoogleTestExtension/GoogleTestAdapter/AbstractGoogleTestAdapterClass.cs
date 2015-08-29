
namespace GoogleTestAdapter
{

    public class AbstractGoogleTestAdapterClass
    {

        private static readonly IOptions OPTIONS = new Options();
        protected virtual IOptions Options { get { return OPTIONS; } }

    }

}
