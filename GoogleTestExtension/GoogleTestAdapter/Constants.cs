namespace GoogleTestAdapter
{

    public class Constants
    {
        internal static bool UnitTestMode = false;

        internal const string FileEndingTestDurations = ".gta_testdurations";

        internal const string IdentifierUri = "executor://GoogleTestRunner/v1";

        public const string TestFinderRegex = @"[Tt]est[s]?\.exe";
    }

}