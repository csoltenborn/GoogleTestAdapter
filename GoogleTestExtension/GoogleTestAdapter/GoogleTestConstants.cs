namespace GoogleTestAdapter
{

    public static class GoogleTestConstants
    {
        public const string SettingsName = "GoogleTestAdapter";
        public const string SettingsExtension = ".gta.runsettings";
        public const string DurationsExtension = ".gta.testdurations";

        public const string AlsoRunDisabledTestsOption = " --gtest_also_run_disabled_tests";
        public const string ShuffleTestsOption = " --gtest_shuffle";
        public const string ShuffleTestsSeedOption = " --gtest_random_seed";
        public const string NrOfRepetitionsOption = " --gtest_repeat";

        public const int ShuffleTestsSeedDefaultValue = 0;
        public const string ShuffleTestsSeedMaxValueAsString = "99999";
        public const int ShuffleTestsSeedMinValue = 0;
        public static readonly int ShuffleTestsSeedMaxValue = int.Parse(ShuffleTestsSeedMaxValueAsString);

        public const string ListTestsOption = " --gtest_list_tests";
        public const string FilterOption = " --gtest_filter=";

        public const string TestBodySignature = "::TestBody";
        public const string ParameterizedTestMarker = "  # GetParam() = ";
        public const string ParameterValueMarker = ".  # TypeParam";

        public static string GetResultXmlFileOption(string resultXmlFile)
        {
            return "--gtest_output=\"xml:" + resultXmlFile + "\"";
        }

        public static string GetTestMethodSignature(string suite, string testCase)
        {
            return suite + "_" + testCase + "_Test" + TestBodySignature;
        }

    }

}