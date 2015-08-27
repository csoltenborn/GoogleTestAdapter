
namespace GoogleTestAdapter
{

    public static class Options
    {
        public const string CATEGORY_NAME = "Google Test Adapter";
        public const string PAGE_NAME = "General";

        private const string REG_OPTION_BASE_ = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0\ApplicationPrivateSettings\GoogleTestAdapterVSIX\OptionPageGrid";
        private const string REG_OPTION_BASE = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0Exp\ApplicationPrivateSettings\GoogleTestAdapterVSIX\OptionPageGrid";

        public const string OPTION_PRINT_TEST_OUTPUT = "Print test output";
        public const string OPTION_TEST_DISCOVERY_REGEX = "Regex for test discovery";
        public const string OPTION_RUN_DISABLED_TESTS = "Also run disabled tests";
        public const string OPTION_NR_OF_TEST_REPETITIONS = "Number of test repetitions";
        public const string OPTION_SHUFFLE_TESTS = "Shuffle tests per execution";

        public const bool OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE = false;
        public const string OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE = "";
        public const bool OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE = false;
        public const int OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE = 1;
        public const bool OPTION_SHUFFLE_TESTS_DEFAULT_VALUE = false;

        private const string REG_OPTION_PRINT_TEST_OUTPUT = "PrintTestOutput";
        private const string REG_OPTION_TEST_DISCOVERY_REGEX = "TestDiscoveryRegex";
        private const string REG_OPTION_RUN_DISABLED_TESTS = "RunDisabledTests";
        private const string REG_OPTION_NR_OF_TEST_REPETITIONS = "NrOfTestRepetitions";
        private const string REG_OPTION_SHUFFLE_TESTS = "ShuffleTests";

        public static bool PrintTestOutput
        {
            get
            {
                return RegistryReader.ReadBool(REG_OPTION_BASE, REG_OPTION_PRINT_TEST_OUTPUT, OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE);
            }
        }

        public static string TestDiscoveryRegex
        {
            get
            {
                return RegistryReader.ReadString(REG_OPTION_BASE, REG_OPTION_TEST_DISCOVERY_REGEX, OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE);
            }
        }

        public static bool RunDisabledTests
        {
            get
            {
                return RegistryReader.ReadBool(REG_OPTION_BASE, REG_OPTION_RUN_DISABLED_TESTS, OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE);
            }
        }

        public static int NrOfTestRepetitions
        {
            get
            {
                return RegistryReader.ReadInt(REG_OPTION_BASE, REG_OPTION_NR_OF_TEST_REPETITIONS, OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE);
            }
        }

        public static bool ShuffleTests
        {
            get
            {
                return RegistryReader.ReadBool(REG_OPTION_BASE, REG_OPTION_SHUFFLE_TESTS, OPTION_SHUFFLE_TESTS_DEFAULT_VALUE);
            }
        }

    }

}