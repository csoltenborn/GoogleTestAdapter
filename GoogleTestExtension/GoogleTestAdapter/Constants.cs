
namespace GoogleTestAdapter
{
    public class Constants
    {
        public static bool UNIT_TEST_MODE = false;

        public const string identifierUri = "executor://GoogleTestRunner/v1";
        public const string gtestListTests = "--gtest_list_tests";
        public const string gtestTestBodySignature = "::TestBody";

        public const string TEST_FINDER_REGEX = @"[Tt]est[s]?\.exe";

    }
}