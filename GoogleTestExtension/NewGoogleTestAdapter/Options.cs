using System;
using System.Collections.Generic;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter
{
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

    public class Options
    {
        public const string TestFinderRegex = @"[Tt]est[s]?\.exe";

        private IXmlOptions XmlOptions { get; }
        private TestEnvironment TestEnvironment { get; }
        private RegexTraitParser RegexTraitParser { get; }


        public Options(IXmlOptions xmlOptions, ILogger logger)
        {
            this.XmlOptions = xmlOptions;
            this.TestEnvironment = new TestEnvironment(this, logger);
            this.RegexTraitParser = new RegexTraitParser(TestEnvironment);
        }

        public Options() { }


        public string GetUserParameters(string solutionDirectory, string testDirectory, int threadId)
        {
            return ReplacePlaceholders(AdditionalTestExecutionParam, solutionDirectory, testDirectory, threadId);
        }

        public string GetBatchForTestSetup(string solutionDirectory, string testDirectory, int threadId)
        {
            return ReplacePlaceholders(BatchForTestSetup, solutionDirectory, testDirectory, threadId);
        }

        public string GetBatchForTestTeardown(string solutionDirectory, string testDirectory, int threadId)
        {
            return ReplacePlaceholders(BatchForTestTeardown, solutionDirectory, testDirectory, threadId);
        }

        private string ReplacePlaceholders(string theString, string solutionDirectory, string testDirectory, int threadId)
        {
            if (string.IsNullOrEmpty(theString))
            {
                return "";
            }

            string result = theString.Replace(Options.TestDirPlaceholder, testDirectory);
            result = result.Replace(Options.ThreadIdPlaceholder, threadId.ToString());
            result = result.Replace(Options.SolutionDirPlaceholder, solutionDirectory);
            return result;
        }


        public const string CategoryName = "Google Test Adapter";
        public const string PageGeneralName = "General";
        public const string PageParallelizationName = "Parallelization";
        public const string PageAdvancedName = "Advanced";

        public const string SolutionDirPlaceholder = "$(SolutionDir)";
        public const string TestDirPlaceholder = "$(TestDir)";
        public const string ThreadIdPlaceholder = "$(ThreadId)";

        private const string DescriptionOfPlaceholders =
           TestDirPlaceholder + " - path of a directory which can be used by the tests\n" +
           ThreadIdPlaceholder + " - id of thread executing the current tests\n" +
           SolutionDirPlaceholder + " - directory of the solution";

        #region GeneralOptionsPage

        public const string OptionPrintTestOutput = "Print test output";
        public const bool OptionPrintTestOutputDefaultValue = false;
        public const string OptionPrintTestOutputDescription =
            "Print the output of the Google Test executable(s) to the Tests Output window.";

        public virtual bool PrintTestOutput => XmlOptions.PrintTestOutput ?? OptionPrintTestOutputDefaultValue;


        public const string OptionTestDiscoveryRegex = "Regex for test discovery";
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public const string OptionTestDiscoveryRegexDescription =
            "If non-empty, this regex will be used to discover the Google Test executables containing your tests.\nDefault regex: "
            + TestFinderRegex;

        public virtual string TestDiscoveryRegex => XmlOptions.TestDiscoveryRegex ?? OptionTestDiscoveryRegexDefaultValue;


        public const string OptionRunDisabledTests = "Also run disabled tests";
        public const bool OptionRunDisabledTestsDefaultValue = false;
        public const string OptionRunDisabledTestsDescription =
            "If true, all (selected) tests will be run, even if they have been disabled.\n"
            + "Google Test option:" + GoogleTestConstants.AlsoRunDisabledTestsOption;

        public virtual bool RunDisabledTests => XmlOptions.RunDisabledTests ?? OptionRunDisabledTestsDefaultValue;


        public const string OptionNrOfTestRepetitions = "Number of test repetitions";
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;
        public const string OptionNrOfTestRepetitionsDescription =
            "Tests will be run for the selected number of times (-1: infinite).\n"
            + "Google Test option:" + GoogleTestConstants.NrOfRepetitionsOption;

        public virtual int NrOfTestRepetitions
        {
            get
            {
                int nrOfRepetitions = XmlOptions.NrOfTestRepetitions ?? OptionNrOfTestRepetitionsDefaultValue;
                if (nrOfRepetitions == 0 || nrOfRepetitions < -1)
                {
                    nrOfRepetitions = OptionNrOfTestRepetitionsDefaultValue;
                }
                return nrOfRepetitions;
            }
        }


        public const string OptionShuffleTests = "Shuffle tests per execution";
        public const bool OptionShuffleTestsDefaultValue = false;
        public const string OptionShuffleTestsDescription =
            "If true, tests will be executed in random order. Note that a true randomized order is only given when executing all tests in non-parallel fashion. Otherwise, the test excutables will most likely be executed more than once - random order is than restricted to the according executions.\n"
            + "Google Test option:" + GoogleTestConstants.ShuffleTestsOption;

        public virtual bool ShuffleTests => XmlOptions.ShuffleTests ?? OptionShuffleTestsDefaultValue;


        public const string OptionShuffleTestsSeed = "Shuffle tests: Seed";
        public const int OptionShuffleTestsSeedDefaultValue = GoogleTestConstants.ShuffleTestsSeedDefaultValue;
        public const string OptionShuffleTestsSeedDescription = "0: Seed is computed from system time, 1<n<"
                                                           + GoogleTestConstants.ShuffleTestsSeedMaxValueAsString
                                                           + ": The given seed is used. See note of option '"
                                                           + OptionShuffleTests
                                                           + "'.";

        public virtual int ShuffleTestsSeed
        {
            get
            {
                int seed = XmlOptions.ShuffleTestsSeed ?? OptionShuffleTestsSeedDefaultValue;
                if (seed < GoogleTestConstants.ShuffleTestsSeedMinValue || seed > GoogleTestConstants.ShuffleTestsSeedMaxValue)
                {
                    seed = OptionShuffleTestsSeedDefaultValue;
                }
                return seed;
            }
        }


        public const string TraitsRegexesPairSeparator = "//||//";
        public const string TraitsRegexesRegexSeparator = "///";
        public const string TraitsRegexesTraitSeparator = ",";
        public const string OptionTraitsRegexesDefaultValue = "";
        public const string OptionTraitsDescription = "Allows to override/add traits for testcases matching a regex. Traits are build up in 3 phases: 1st, traits are assigned to tests according to the 'Traits before' option. 2nd, the tests' traits (defined via the macros in GTA_Traits.h) are added to the tests, overriding traits from phase 1 with new values. 3rd, the 'Traits after' option is evaluated, again in an overriding manner.\nSyntax: "
                                                 + TraitsRegexesRegexSeparator +
                                                 " separates the regex from the traits, the trait's name and value are separated by "
                                                 + TraitsRegexesTraitSeparator +
                                                 " and each pair of regex and trait is separated by "
                                                 + TraitsRegexesPairSeparator + ".\nExample: " +
                                                 @"MySuite\.*"
                                                 + TraitsRegexesRegexSeparator + "Type"
                                                 + TraitsRegexesTraitSeparator + "Small"
                                                 + TraitsRegexesPairSeparator +
                                                 @"MySuite2\.*|MySuite3\.*"
                                                 + TraitsRegexesRegexSeparator + "Type"
                                                 + TraitsRegexesTraitSeparator + "Medium";

        public const string OptionTraitsRegexesBefore = "Regex for setting test traits before test execution";

        public virtual List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = XmlOptions.TraitsRegexesBefore ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }

        public const string OptionTraitsRegexesAfter = "Regex for setting test traits after test execution";

        public virtual List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = XmlOptions.TraitsRegexesAfter ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }


        public const string OptionDebugMode = "Debug mode";
        public const bool OptionDebugModeDefaultValue = false;
        public const string OptionDebugModeDescription =
            "If true, debug output will be printed to the test console.";

        public virtual bool DebugMode => XmlOptions.DebugMode ?? OptionDebugModeDefaultValue;


        public const string OptionAdditionalTestExecutionParams = "Additional test execution parameters";
        public const string OptionAdditionalTestExecutionParamsDefaultValue = "";
        public const string OptionAdditionalTestExecutionParamsDescription =
            "Additional parameters for Google Test executable. Placeholders:\n"
            + DescriptionOfPlaceholders;

        public virtual string AdditionalTestExecutionParam => XmlOptions.AdditionalTestExecutionParam ?? OptionAdditionalTestExecutionParamsDefaultValue;

        #endregion

        #region ParallelizationOptionsPage

        public const string OptionEnableParallelTestExecution = "Enable parallel test execution";
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;
        public const string OptionEnableParallelTestExecutionDescription =
            "Parallel test execution is achieved by means of different threads, each of which is assigned a number of tests to be executed. The threads will then sequentially invoke the necessary executables to produce the according test results.";

        public virtual bool ParallelTestExecution => XmlOptions.ParallelTestExecution ?? OptionEnableParallelTestExecutionDefaultValue;


        public const string OptionMaxNrOfThreads = "Maximum number of threads";
        public const int OptionMaxNrOfThreadsDefaultValue = 0;
        public const string OptionMaxNrOfThreadsDescription =
            "Maximum number of threads to be used for test execution (0: all available threads).";

        public virtual int MaxNrOfThreads
        {
            get
            {
                int result = XmlOptions.MaxNrOfThreads ?? OptionMaxNrOfThreadsDefaultValue;
                if (result <= 0 || result > Environment.ProcessorCount)
                {
                    result = Environment.ProcessorCount;
                }
                return result;
            }
        }


        public const string OptionBatchForTestSetup = "Test setup batch file";
        public const string OptionBatchForTestSetupDefaultValue = "";
        public const string OptionBatchForTestSetupDescription =
            "Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n"
            + DescriptionOfPlaceholders;

        public virtual string BatchForTestSetup => XmlOptions.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;


        public const string OptionBatchForTestTeardown = "Test teardown batch file";
        public const string OptionBatchForTestTeardownDefaultValue = "";
        public const string OptionBatchForTestTeardownDescription =
            "Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n"
            + DescriptionOfPlaceholders;

        public virtual string BatchForTestTeardown => XmlOptions.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;

        #endregion

        #region AdvancedOptionsPage

        public const string OptionReportWaitPeriod = "Wait period during result reporting";
        public const int OptionReportWaitPeriodDefaultValue = 0;
        public const string OptionReportWaitPeriodDescription =
            "Sometimes, not all TestResults are recognized by VS. This is probably due to inter process communication - if anybody has a clean solution for this, please provide a patch. Until then, use this option to ovetcome such problems.\n" +
            "During test reporting, 0: do not pause at all, n: pause for 1ms every nth test (the higher, the faster; 1 is slowest)";

        public virtual int ReportWaitPeriod
        {
            get
            {
                int period = XmlOptions.ReportWaitPeriod ?? OptionReportWaitPeriodDefaultValue;
                if (period < 0)
                {
                    period = OptionReportWaitPeriodDefaultValue;
                }
                return period;
            }
        }


        public const string OptionDevelopmentMode = "Development mode";
        public const bool OptionDevelopmentModeDefaultValue = false;
        public const string OptionDevelopmentModeDescription =
            "If true, dialogs will open which help to debug test discovery and execution code (which is executed in processes different to the one Visual Studio runs in).";

        public virtual bool DevelopmentMode => XmlOptions.DevelopmentMode ?? OptionDevelopmentModeDefaultValue;

        #endregion

    }

}