
using EnvDTE;
using System;
using System.Windows.Forms;

namespace GoogleTestAdapter
{

    public static class Options
    {
        public const string CATEGORY_NAME = "Google Test Adapter";
        public const string PAGE_NAME = "General";

        public const string OPTION_PRINT_TEST_OUTPUT = "Print test output";
        public const bool OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE = true;
        public const string OPTION_TEST_DISCOVERY_REGEX = "Regex for test discovery";
        public const string OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE = "";
        public const string OPTION_RUN_DISABLED_TESTS = "Also run disabled tests";
        public const bool OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE = false;
        public const string OPTION_NR_OF_TEST_REPETITIONS = "Number of test repetitions";
        public const int OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE = 1;
        public const string OPTION_SHUFFLE_TESTS = "Shuffle tests per execution";
        public const bool OPTION_SHUFFLE_TESTS_DEFAULT_VALUE = false;

        public static bool PrintTestOutput
        {
            get
            {
                return GetPropertyValue(OPTION_PRINT_TEST_OUTPUT, OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE);
            }
        }

        public static string TestDiscoveryRegex
        {
            get
            {
                return GetPropertyValue(OPTION_TEST_DISCOVERY_REGEX, OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE);
            }
        }

        public static bool RunDisabledTests
        {
            get
            {
                return GetPropertyValue(OPTION_RUN_DISABLED_TESTS, OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE);
            }
        }

        public static int NrOfTestRepetitions
        {
            get
            {
                return GetPropertyValue(OPTION_NR_OF_TEST_REPETITIONS, OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE);
            }
        }

        public static bool ShuffleTests
        {
            get
            {
                return GetPropertyValue(OPTION_SHUFFLE_TESTS, OPTION_SHUFFLE_TESTS_DEFAULT_VALUE);
            }
        }

        private static T GetPropertyValue<T>(string propertyName, T defaultValue)
        {
            try
            {
                Properties properties = DTEProvider.DTE.Properties[CATEGORY_NAME, PAGE_NAME];
                Property TheProperty = properties.Item(propertyName);
                T Result = TheProperty.Value;
                MessageBox.Show("Here we go: value = " + Result);
                return Result;
            }
            catch
            {
                return defaultValue;
            }
        }

    }

}