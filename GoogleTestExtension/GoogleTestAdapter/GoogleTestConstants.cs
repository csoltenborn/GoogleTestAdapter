namespace GoogleTestAdapter
{
    public static class GoogleTestConstants
    {

        internal const string ListTestsOption = "--gtest_list_tests";
        internal const string FilterOption = " --gtest_filter=";
        public const string AlsoRunDisabledTestsOption = " --gtest_also_run_disabled_tests";
        public const string ShuffleTestsOption = " --gtest_shuffle";
        public const string NrOfRepetitionsOption = " --gtest_repeat=";

        internal const string TestBodySignature = "::TestBody";
        internal const string ParameterizedTestMarker = "# GetParam() = ";
        internal const string ParameterValueMarker = ".  # TypeParam";

        internal static string GetResultXmlFileOption(string resultXmlFile)
        {
            return "--gtest_output=\"xml:" + resultXmlFile + "\"";
        }

        internal static string GetTestMethodSignature(string suite, string testCase)
        {
            return suite + "_" + testCase + "_Test" + TestBodySignature;
        }

    }

}