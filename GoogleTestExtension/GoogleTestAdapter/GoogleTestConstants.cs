namespace GoogleTestAdapter
{
    static class GoogleTestConstants
    {

        internal const string ListTestsOption = "--gtest_list_tests";
        internal const string FilterOption = " --gtest_filter=";
        internal const string AlsoRunDisabledTestsOption = " --gtest_also_run_disabled_tests";
        internal const string ShuffleTestsOption = " --gtest_shuffle";
        internal const string NrOfRepetitionsOption = " --gtest_repeat=";

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