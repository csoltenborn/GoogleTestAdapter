namespace GoogleTestAdapter
{

    public class Constants
    {
        internal static bool UnitTestMode = false;

        internal const string FileEndingTestDurations = ".gta_testdurations";

        internal const string IdentifierUri = "executor://GoogleTestRunner/v1";
        internal const string GtestListTests = "--gtest_list_tests";
        internal const string GtestTestBodySignature = "::TestBody";

        public const string TestFinderRegex = @"[Tt]est[s]?\.exe";
    }

}