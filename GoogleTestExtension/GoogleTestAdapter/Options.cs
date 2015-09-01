
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    public class Options : IOptions
    {
        public const string CATEGORY_NAME = "Google Test Adapter";
        public const string PAGE_NAME = "General";

        // ReSharper disable once UnusedMember.Local
        private const string REG_OPTION_BASE_PRODUCTION = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0\ApplicationPrivateSettings\GoogleTestAdapterVSIX\OptionPageGrid";
        // ReSharper disable once UnusedMember.Local
        private const string REG_OPTION_BASE_DEBUGGING = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0Exp\ApplicationPrivateSettings\GoogleTestAdapterVSIX\OptionPageGrid";
        private const string REG_OPTION_BASE = REG_OPTION_BASE_PRODUCTION;

        public const string OPTION_PRINT_TEST_OUTPUT = "Print test output";
        public const string OPTION_TEST_DISCOVERY_REGEX = "Regex for test discovery";
        public const string OPTION_RUN_DISABLED_TESTS = "Also run disabled tests";
        public const string OPTION_NR_OF_TEST_REPETITIONS = "Number of test repetitions";
        public const string OPTION_SHUFFLE_TESTS = "Shuffle tests per execution";
        public const string OPTION_TRAITS_REGEXES_BEFORE = "Regex for setting test traits before test execution";
        public const string OPTION_TRAITS_REGEXES_AFTER = "Regex for setting test traits after test execution";

        public const bool OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE = false;
        public const string OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE = "";
        public const bool OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE = false;
        public const int OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE = 1;
        public const bool OPTION_SHUFFLE_TESTS_DEFAULT_VALUE = false;
        public const string OPTION_TRAITS_REGEXES_DEFAULT_VALUE = "";

        private const string REG_OPTION_PRINT_TEST_OUTPUT = "PrintTestOutput";
        private const string REG_OPTION_TEST_DISCOVERY_REGEX = "TestDiscoveryRegex";
        private const string REG_OPTION_RUN_DISABLED_TESTS = "RunDisabledTests";
        private const string REG_OPTION_NR_OF_TEST_REPETITIONS = "NrOfTestRepetitions";
        private const string REG_OPTION_SHUFFLE_TESTS = "ShuffleTests";
        private const string REG_OPTION_TRAITS_REGEXES_BEFORE = "TraitsRegexesBefore";
        private const string REG_OPTION_TRAITS_REGEXES_AFTER = "TraitsRegexesAfter";

        public const string TRAITS_REGEXES_PAIR_SEPARATOR = "//||//";
        public const string TRAITS_REGEXES_REGEX_SEPARATOR = "///";
        public const string TRAITS_REGEXES_TRAIT_SEPARATOR = ",";

        public bool PrintTestOutput => RegistryReader.ReadBool(REG_OPTION_BASE, REG_OPTION_PRINT_TEST_OUTPUT, OPTION_PRINT_TEST_OUTPUT_DEFAULT_VALUE);

        public string TestDiscoveryRegex => RegistryReader.ReadString(REG_OPTION_BASE, REG_OPTION_TEST_DISCOVERY_REGEX, OPTION_TEST_DISCOVERY_REGEX_DEFAULT_VALUE);

        public bool RunDisabledTests => RegistryReader.ReadBool(REG_OPTION_BASE, REG_OPTION_RUN_DISABLED_TESTS, OPTION_RUN_DISABLED_TESTS_DEFAULT_VALUE);

        public int NrOfTestRepetitions => RegistryReader.ReadInt(REG_OPTION_BASE, REG_OPTION_NR_OF_TEST_REPETITIONS, OPTION_NR_OF_TEST_REPETITIONS_DEFAULT_VALUE);

        public bool ShuffleTests => RegistryReader.ReadBool(REG_OPTION_BASE, REG_OPTION_SHUFFLE_TESTS, OPTION_SHUFFLE_TESTS_DEFAULT_VALUE);

        public List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string Option = RegistryReader.ReadString(REG_OPTION_BASE, REG_OPTION_TRAITS_REGEXES_BEFORE, OPTION_TRAITS_REGEXES_DEFAULT_VALUE);
                return ParseTraitsRegexesString(Option);
            }
        }

        public List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string Option = RegistryReader.ReadString(REG_OPTION_BASE, REG_OPTION_TRAITS_REGEXES_AFTER, OPTION_TRAITS_REGEXES_DEFAULT_VALUE);
                return ParseTraitsRegexesString(Option);
            }
        }

        private List<RegexTraitPair> ParseTraitsRegexesString(string option)
        {
            List<RegexTraitPair> Result = new List<RegexTraitPair>();
            string[] Pairs = option.Split(new[] { TRAITS_REGEXES_PAIR_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string Pair in Pairs)
            {
                try
                {
                    string[] Values = Pair.Split(new[] { TRAITS_REGEXES_REGEX_SEPARATOR }, StringSplitOptions.None);
                    string[] Trait = Values[1].Split(new[] { TRAITS_REGEXES_TRAIT_SEPARATOR }, StringSplitOptions.None);
                    string Regex = Values[0];
                    string TraitName = Trait[0];
                    string TraitValue = Trait[1];
                    Result.Add(new RegexTraitPair(Regex, TraitName, TraitValue));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Could not parse pair '" + Pair + "', exception message: " + e.Message);
                }
            }
            return Result;
        }

    }

}