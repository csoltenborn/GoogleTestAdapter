// This file has been modified by Microsoft on 4/2020.

using System;

namespace GoogleTestAdapter
{

    public static class GoogleTestConstants
    {
        public const string TestPropertySettingsName = "TestPropertySettingsForGoogleAdapter";
        public const string SettingsName = "GoogleTestAdapterSettings";
        public const string SettingsExtension = ".gta.runsettings";
        public const string DurationsExtension = ".gta.testdurations";

        public const string AlsoRunDisabledTestsOption = " --gtest_also_run_disabled_tests";
        public const string ShuffleTestsOption = " --gtest_shuffle";
        public const string ShuffleTestsSeedOption = " --gtest_random_seed";
        public const string NrOfRepetitionsOption = " --gtest_repeat";
        public const string CatchExceptions = " --gtest_catch_exceptions";
        public const string BreakOnFailure = " --gtest_break_on_failure";

        public const int ShuffleTestsSeedDefaultValue = 0;
        public const string ShuffleTestsSeedMaxValueAsString = "99999";
        public const int ShuffleTestsSeedMinValue = 0;
        public static readonly int ShuffleTestsSeedMaxValue = int.Parse(ShuffleTestsSeedMaxValueAsString);

        public const string ListTestsOption = "--gtest_list_tests";
        public const string FilterOption = " --gtest_filter=";

        public const string TestBodySignature = "::TestBody";
        public const string ParameterizedTestMarker = "  # GetParam() = ";
        public const string TypedTestMarker = ".  # TypeParam = ";

        public const string GoogleTestDllMarker = "gtest.dll";
        public const string GoogleTestDllMarkerDebug = "gtestd.dll";
        public const string GoogleTestMainDllMarker = "gtest_main.dll";
        public const string GoogleTestMainDllMarkerDebug = "gtest_maind.dll";

        public static readonly string[] GoogleTestExecutableMarkers =
        {
            "This program contains tests written using Google Test. You can use the",
            "For more information, please read the Google Test documentation at",
            "Run only the tests whose name matches one of the positive patterns but",
            ListTestsOption
        };

        public static string GetCatchExceptionsOption(bool catchThem)
        {
            int optionValue = catchThem ? 1 : 0;
            return $"{CatchExceptions}={optionValue}";
        }

        public static string GetBreakOnFailureOption(bool doBreak)
        {
            int optionValue = doBreak ? 1 : 0;
            return $"{BreakOnFailure}={optionValue}";
        }

        public static void ValidateShuffleTestsSeedValue(int value)
        {
            if (value < ShuffleTestsSeedMinValue || value > ShuffleTestsSeedMaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"Expected a number between {ShuffleTestsSeedMinValue} and {ShuffleTestsSeedMaxValue}, inclusive.");
        }

    }

}