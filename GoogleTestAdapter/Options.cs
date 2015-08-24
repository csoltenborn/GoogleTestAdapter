
namespace GoogleTestAdapter
{

    public static class Options
    {
        public const string CATEGORY_NAME = "Google Test Adapter";
        public const string PAGE_NAME = "General";

        public static bool PrintTestOutput()
        {
            return true;
        }

        public static string TestDiscoveryRegex()
        {
            return "";
        }

        public static bool RunDisabledTests()
        {
            return false;
        }

        public static int NrOfTestRepetitions()
        {
            return 1;
        }

        public static bool ShuffleTests()
        {
            return false;
        }

    }

}