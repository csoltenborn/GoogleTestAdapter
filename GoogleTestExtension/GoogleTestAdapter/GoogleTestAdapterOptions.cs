using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{

    public abstract class AbstractOptions
    {
        public abstract bool PrintTestOutput { get; }
        public abstract string TestDiscoveryRegex { get; }
        public abstract bool RunDisabledTests { get; }
        public abstract int NrOfTestRepetitions { get; }
        public abstract bool ShuffleTests { get; }
        public abstract List<RegexTraitPair> TraitsRegexesBefore { get; }
        public abstract List<RegexTraitPair> TraitsRegexesAfter { get; }
        public abstract bool UserDebugMode { get; }
        public abstract int TestCounter { get; }

        public abstract bool ParallelTestExecution { get; }
        public abstract int MaxNrOfThreads { get; }
        public abstract string TestSetupBatch { get; }
        public abstract string TestTeardownBatch { get; }
        public abstract string AdditionalTestExecutionParam { get; }

        internal string GetUserParameters(string solutionDirectory, string testDirectory, int threadId)
        {
            return ReplacePlaceholders(AdditionalTestExecutionParam, solutionDirectory, testDirectory, threadId);
        }

        internal string GetTestSetupBatch(string solutionDirectory, string testDirectory, int threadId)
        {
            return ReplacePlaceholders(TestSetupBatch, solutionDirectory, testDirectory, threadId);
        }

        internal string GetTestTeardownBatch(string solutionDirectory, string testDirectory, int threadId)
        {
            return ReplacePlaceholders(TestTeardownBatch, solutionDirectory, testDirectory, threadId);
        }

        private static string ReplacePlaceholders(string theString, string solutionDirectory, string testDirectory, int threadId)
        {
            if (string.IsNullOrEmpty(theString))
            {
                return "";
            }

            string result = theString.Replace(GoogleTestAdapterOptions.TestDirPlaceholder, testDirectory);
            result = result.Replace(GoogleTestAdapterOptions.ThreadIdPlaceholder, threadId.ToString());
            result = result.Replace(GoogleTestAdapterOptions.SolutionDirPlaceholder, solutionDirectory);
            return result;
        }

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

    public class GoogleTestAdapterOptions : AbstractOptions
    {
        internal GoogleTestAdapterOptions() { }

        public const string CategoryName = "Google Test Adapter";
        public const string PageGeneralName = "General";
        public const string PageParallelizationName = "Parallelization (experimental)";

        // ReSharper disable once UnusedMember.Local
        private const string RegOptionBaseProduction = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0\ApplicationPrivateSettings\GoogleTestAdapterVSIX";
        // ReSharper disable once UnusedMember.Local
        private const string RegOptionBaseDebugging = @"HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\14.0Exp\ApplicationPrivateSettings\GoogleTestAdapterVSIX";
        private const string RegOptionGeneralBase = RegOptionBaseProduction + @"\GeneralOptionsDialogPage";
        private const string RegOptionParallelizationBase = RegOptionBaseProduction + @"\ParallelizationOptionsDialogPage";

        //\OptionPageGrid
        public const string OptionPrintTestOutput = "Print test output";
        public const string OptionTestDiscoveryRegex = "Regex for test discovery";
        public const string OptionRunDisabledTests = "Also run disabled tests";
        public const string OptionNrOfTestRepetitions = "Number of test repetitions";
        public const string OptionShuffleTests = "Shuffle tests per execution";
        public const string OptionTraitsRegexesBefore = "Regex for setting test traits before test execution";
        public const string OptionTraitsRegexesAfter = "Regex for setting test traits after test execution";
        public const string OptionUserDebugMode = "Debug mode";
        public const string OptionEnableParallelTestExecution = "Enable parallel test execution";
        public const string OptionMaxNrOfThreads = "Maximum number of threads";
        public const string OptionTestSetupBatch = "Test setup batch file";
        public const string OptionTestTeardownBatch = "Test teardown batch file";
        public const string OptionAdditionalTestExecutionParam = "Additional test execution parameters";

        public const bool OptionPrintTestOutputDefaultValue = false;
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public const bool OptionRunDisabledTestsDefaultValue = false;
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;
        public const bool OptionShuffleTestsDefaultValue = false;
        public const string OptionTraitsRegexesDefaultValue = "";
        public const bool OptionUserDebugModeDefaultValue = false;
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;
        public const int OptionMaxNrOfThreadsDefaultValue = 0;
        public const string OptionTestSetupBatchDefaultValue = "";
        public const string OptionTestTeardownBatchDefaultValue = "";
        public const string OptionAdditionalTestExecutionParamDefaultValue = "";

        private const string RegOptionPrintTestOutput = "PrintTestOutput";
        private const string RegOptionTestDiscoveryRegex = "TestDiscoveryRegex";
        private const string RegOptionRunDisabledTests = "RunDisabledTests";
        private const string RegOptionNrOfTestRepetitions = "NrOfTestRepetitions";
        private const string RegOptionShuffleTests = "ShuffleTests";
        private const string RegOptionTraitsRegexesBefore = "TraitsRegexesBefore";
        private const string RegOptionTraitsRegexesAfter = "TraitsRegexesAfter";
        private const string RegOptionUserDebugMode = "UserDebugMode";
        private const string RegOptionEnableParallelTestExecution = "EnableParallelTestExecution";
        private const string RegOptionMaxNrOfThreads = "MaxNumberOfThreads";
        private const string RegOptionTestSetupBatch = "BatchForTestSetup";
        private const string RegOptionTestTeardownBatch = "BatchForTestTeardown";
        private const string RegOptionAdditionalTestExecutionParam = "AdditionalTestExecutionParams";

        public const string TraitsRegexesPairSeparator = "//||//";
        public const string TraitsRegexesRegexSeparator = "///";
        public const string TraitsRegexesTraitSeparator = ",";

        public const string SolutionDirPlaceholder = "${SolutionDir}";
        public const string TestDirPlaceholder = "${TestDir}";
        public const string ThreadIdPlaceholder = "${ThreadId}";

        public const string DescriptionOfPlaceholders =
           TestDirPlaceholder + " - path of a directory which can be used by the tests\n" +
           ThreadIdPlaceholder + " - id of thread executing the current tests\n" + 
           SolutionDirPlaceholder + " - directory of the solution";

        public override bool PrintTestOutput => RegistryReader.ReadBool(RegOptionGeneralBase, RegOptionPrintTestOutput, OptionPrintTestOutputDefaultValue);

        public override string TestDiscoveryRegex => RegistryReader.ReadString(RegOptionGeneralBase, RegOptionTestDiscoveryRegex, OptionTestDiscoveryRegexDefaultValue);

        public override bool RunDisabledTests => RegistryReader.ReadBool(RegOptionGeneralBase, RegOptionRunDisabledTests, OptionRunDisabledTestsDefaultValue);

        public override int NrOfTestRepetitions => RegistryReader.ReadInt(RegOptionGeneralBase, RegOptionNrOfTestRepetitions, OptionNrOfTestRepetitionsDefaultValue);

        public override bool ShuffleTests => RegistryReader.ReadBool(RegOptionGeneralBase, RegOptionShuffleTests, OptionShuffleTestsDefaultValue);

        public override bool UserDebugMode => RegistryReader.ReadBool(RegOptionGeneralBase, RegOptionUserDebugMode, OptionUserDebugModeDefaultValue);

        public override string AdditionalTestExecutionParam => RegistryReader.ReadString(RegOptionGeneralBase, RegOptionAdditionalTestExecutionParam, OptionAdditionalTestExecutionParamDefaultValue);

        public override int TestCounter => RegistryReader.ReadInt(RegOptionGeneralBase, "TestCounter", 1);

        public override List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = RegistryReader.ReadString(RegOptionGeneralBase, RegOptionTraitsRegexesBefore, OptionTraitsRegexesDefaultValue);
                return ParseTraitsRegexesString(option);
            }
        }

        public override List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = RegistryReader.ReadString(RegOptionGeneralBase, RegOptionTraitsRegexesAfter, OptionTraitsRegexesDefaultValue);
                return ParseTraitsRegexesString(option);
            }
        }


        public override bool ParallelTestExecution => RegistryReader.ReadBool(RegOptionParallelizationBase, RegOptionEnableParallelTestExecution, OptionEnableParallelTestExecutionDefaultValue);

        public override string TestSetupBatch => RegistryReader.ReadString(RegOptionParallelizationBase, RegOptionTestSetupBatch, OptionTestSetupBatchDefaultValue);

        public override string TestTeardownBatch => RegistryReader.ReadString(RegOptionParallelizationBase, RegOptionTestTeardownBatch, OptionTestTeardownBatchDefaultValue);

        public override int MaxNrOfThreads
        {
            get
            {
                int result = RegistryReader.ReadInt(RegOptionParallelizationBase, RegOptionMaxNrOfThreads, OptionMaxNrOfThreadsDefaultValue);
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
            string[] pairs = option.Split(new[] { TraitsRegexesPairSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                try
                {
                    string[] values = pair.Split(new[] { TraitsRegexesRegexSeparator }, StringSplitOptions.None);
                    string[] trait = values[1].Split(new[] { TraitsRegexesTraitSeparator }, StringSplitOptions.None);
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