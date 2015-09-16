namespace GoogleTestAdapter
{

    public class Constants
    {
        public static bool UnitTestMode = false;

        public const string FileEndingTestDurations = ".gta_testdurations";

        public const string IdentifierUri = "executor://GoogleTestRunner/v1";
        public const string GtestListTests = "--gtest_list_tests";
        public const string GtestTestBodySignature = "::TestBody";

        public const string TestFinderRegex = @"[Tt]est[s]?\.exe";
    }

}