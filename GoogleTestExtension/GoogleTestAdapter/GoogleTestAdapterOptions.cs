using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{

    public interface IOptions
    {
        bool PrintTestOutput { get; }
        string TestDiscoveryRegex { get; }
        bool RunDisabledTests { get; }
        int NrOfTestRepetitions { get; }
        bool ShuffleTests { get; }
        List<RegexTraitPair> TraitsRegexesBefore { get; }
        List<RegexTraitPair> TraitsRegexesAfter { get; }
        bool UserDebugMode { get; }

        bool ParallelTestExecution { get; }
        int MaxNrOfThreads { get; }
        string TestSetupBatch { get; }
        string TestTeardownBatch { get; }
        string AdditionalTestExecutionParam { get; }
    }

    public class RegexTraitPair
    {
        public string Regex { get; set; }
        public Trait Trait { get; set; }

        public RegexTraitPair(string regex, string name, string value)
        {
            this.Regex = regex;
            this.Trait = new Trait(name, value);
        }
    }

    public class GoogleTestAdapterOptions : IOptions
    {
        public const string CATEGORY_NAME = "Google Test Adapter";
        public const string PAGE_GENERAL_NAME = "General";
        public const string PAGE_PARALLELIZATION_NAME = "Parallelization (experimental)";

        // ReSharper disable once UnusedMember.Local
        private const string REG_OPTION_BASE_PRODUCTION = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0\ApplicationPrivateSettings\GoogleTestAdapterVSIX";
        // ReSharper disable once UnusedMember.Local
        private const string REG_OPTION_BASE_DEBUGGING = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0Exp\ApplicationPrivateSettings\GoogleTestAdapterVSIX";
        private const string REG_OPTION_GENERAL_BASE = REG_OPTION_BASE_PRODUCTION + @"\GeneralOptionsDialogPage";
        private const string REG_OPTION_PARALLELIZATION_BASE = REG_OPTION_BASE_PRODUCTION + @"\ParallelizationOptionsDialogPage";

        //\OptionPageGrid
        public const string OPTION_PRINT_TEST_OUTPUT = "Print test output";
        public const string OPTION_TEST_DISCOVERY_REGEX = "Regex for test discovery";
        public const string OPTION_RUN_DISABLED_TESTS = "Also run disabled tests";
        public const string OPTION_NR_OF_TEST_REPETITIONS = "Number of test repetitions";
        public const string OPTION_SHUFFLE_TESTS = "Shuffle tests per execution";
        public const string OPTION_TRAITS_REGEXES_BEFORE = "Regex for setting test traits before test execution";
        public const string OPTION_TRAITS_REGEXES_AFTER = "Regex for setting test traits after test execution";
        public const string OPTION_USER_DEBUG_MODE = "Debug mode";
        public const string OPTION_ENABLE_PARALLEL_TEST_EXECUTION = "Enable parallel test execution";
        public const string OPTION_MAX_NR_OF_THREADS = "Maximum number of threads to be used";
        public const string OPTION_TEST_SETUP_BATCH = "Batch file for test setup";
        public const string OPTION_TEST_TEARDOWN_BATCH = "Batch file for test teardown";
        public const string OPTION_ADDITIONAL_TEST_EXECUTION_PARAM = "Additional test execution parameters";

        public const bool OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE = false;
        public const string OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE = "";
        public const bool OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE = false;
        public const int OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE = 1;
        public const bool OPTION_SHUFFLE_TESTS_DEFAULT_VALUE = false;
        public const string OPTION_TRAITS_REGEXES_DEFAULT_VALUE = "";
        public const bool OPTION_USER_DEBUG_MODE_DEFAULT_VALUE = false;
        public const bool OPTION_ENABLE_PARALLEL_TEST_EXECUTION_DEFAULT_VALUE = false;
        public const int OPTION_MAX_NR_OF_THREADS_DEFAULT_VALUE = 0;
        public const string OPTION_TEST_SETUP_BATCH_DEFAULT_VALUE = "";
        public const string OPTION_TEST_TEARDOWN_BATCH_DEFAULT_VALUE = "";
        public const string OPTION_ADDITIONAL_TEST_EXECUTION_PARAM_DEFAULT_VALUE = "";

        private const string REG_OPTION_PRINT_TEST_OUTPUT = "PrintTestOutput";
        private const string REG_OPTION_TEST_DISCOVERY_REGEX = "TestDiscoveryRegex";
        private const string REG_OPTION_RUN_DISABLED_TESTS = "RunDisabledTests";
        private const string REG_OPTION_NR_OF_TEST_REPETITIONS = "NrOfTestRepetitions";
        private const string REG_OPTION_SHUFFLE_TESTS = "ShuffleTests";
        private const string REG_OPTION_TRAITS_REGEXES_BEFORE = "TraitsRegexesBefore";
        private const string REG_OPTION_TRAITS_REGEXES_AFTER = "TraitsRegexesAfter";
        private const string REG_OPTION_USER_DEBUG_MODE = "UserDebugMode";
        private const string REG_OPTION_ENABLE_PARALLEL_TEST_EXECUTION = "EnableParallelTestExecution";
        private const string REG_OPTION_MAX_NR_OF_THREADS = "MaxNumberOfThreads";
        private const string REG_OPTION_TEST_SETUP_BATCH = "BatchForTestSetup";
        private const string REG_OPTION_TEST_TEARDOWN_BATCH = "BatchForTestTeardown";
        private const string REG_OPTION_ADDITIONAL_TEST_EXECUTION_PARAM = "AdditionalTestExecutionParams";

        public const string TRAITS_REGEXES_PAIR_SEPARATOR = "//||//";
        public const string TRAITS_REGEXES_REGEX_SEPARATOR = "///";
        public const string TRAITS_REGEXES_TRAIT_SEPARATOR = ",";

        public static string ReplacePlaceholders(string source, string testDirectory)
        {
            string result = source.Replace("${TestDirectory}", testDirectory);
            return result;
        }

        public const string DESCRIPTION_OF_PLACEHOLDERS = "${TestDirectory} - path of a directory which can be used by the tests";

        public bool PrintTestOutput => RegistryReader.ReadBool(REG_OPTION_GENERAL_BASE, REG_OPTION_PRINT_TEST_OUTPUT, OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE);

        public string TestDiscoveryRegex => RegistryReader.ReadString(REG_OPTION_GENERAL_BASE, REG_OPTION_TEST_DISCOVERY_REGEX, OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE);

        public bool RunDisabledTests => RegistryReader.ReadBool(REG_OPTION_GENERAL_BASE, REG_OPTION_RUN_DISABLED_TESTS, OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE);

        public int NrOfTestRepetitions => RegistryReader.ReadInt(REG_OPTION_GENERAL_BASE, REG_OPTION_NR_OF_TEST_REPETITIONS, OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE);

        public bool ShuffleTests => RegistryReader.ReadBool(REG_OPTION_GENERAL_BASE, REG_OPTION_SHUFFLE_TESTS, OPTION_SHUFFLE_TESTS_DEFAULT_VALUE);

        public bool UserDebugMode => RegistryReader.ReadBool(REG_OPTION_GENERAL_BASE, REG_OPTION_USER_DEBUG_MODE, OPTION_USER_DEBUG_MODE_DEFAULT_VALUE);

        public string AdditionalTestExecutionParam => RegistryReader.ReadString(REG_OPTION_GENERAL_BASE, REG_OPTION_ADDITIONAL_TEST_EXECUTION_PARAM, OPTION_ADDITIONAL_TEST_EXECUTION_PARAM_DEFAULT_VALUE);

        public List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = RegistryReader.ReadString(REG_OPTION_GENERAL_BASE, REG_OPTION_TRAITS_REGEXES_BEFORE, OPTION_TRAITS_REGEXES_DEFAULT_VALUE);
                return ParseTraitsRegexesString(option);
            }
        }

        public List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = RegistryReader.ReadString(REG_OPTION_GENERAL_BASE, REG_OPTION_TRAITS_REGEXES_AFTER, OPTION_TRAITS_REGEXES_DEFAULT_VALUE);
                return ParseTraitsRegexesString(option);
            }
        }


        public bool ParallelTestExecution => RegistryReader.ReadBool(REG_OPTION_PARALLELIZATION_BASE, REG_OPTION_ENABLE_PARALLEL_TEST_EXECUTION, OPTION_ENABLE_PARALLEL_TEST_EXECUTION_DEFAULT_VALUE);

        public string TestSetupBatch => RegistryReader.ReadString(REG_OPTION_PARALLELIZATION_BASE, REG_OPTION_TEST_SETUP_BATCH, OPTION_TEST_SETUP_BATCH_DEFAULT_VALUE);

        public string TestTeardownBatch => RegistryReader.ReadString(REG_OPTION_PARALLELIZATION_BASE, REG_OPTION_TEST_TEARDOWN_BATCH, OPTION_TEST_TEARDOWN_BATCH_DEFAULT_VALUE);

        public int MaxNrOfThreads
        {
            get
            {
                int result = RegistryReader.ReadInt(REG_OPTION_PARALLELIZATION_BASE, REG_OPTION_MAX_NR_OF_THREADS, OPTION_MAX_NR_OF_THREADS_DEFAULT_VALUE);
                if (result <= 0 || result > Environment.ProcessorCount)
                {
                    result = Environment.ProcessorCount;
                }
                return result;
            }
        }


        private List<RegexTraitPair> ParseTraitsRegexesString(string option)
        {
            List<RegexTraitPair> result = new List<RegexTraitPair>();
            string[] pairs = option.Split(new[] { TRAITS_REGEXES_PAIR_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                try
                {
                    string[] values = pair.Split(new[] { TRAITS_REGEXES_REGEX_SEPARATOR }, StringSplitOptions.None);
                    string[] trait = values[1].Split(new[] { TRAITS_REGEXES_TRAIT_SEPARATOR }, StringSplitOptions.None);
                    string regex = values[0];
                    string traitName = trait[0];
                    string traitValue = trait[1];
                    result.Add(new RegexTraitPair(regex, traitName, traitValue));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Could not parse pair '" + pair + "', exception message: " + e.Message);
                }
            }
            return result;
        }

    }

}