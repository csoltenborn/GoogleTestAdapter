
namespace GoogleTestAdapter
{

    public class AbstractGoogleTestAdapterClass
    {

        private IOptions options = new Options();
        protected virtual IOptions Options { get { return options; } set { options = value; } }

    }

}
