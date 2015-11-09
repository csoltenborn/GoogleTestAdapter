using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml;
using System.Xml.XPath;

namespace GoogleTestAdapter
{
    public interface IGlobalRunSettings
    {
        RunSettings RunSettings { get; }
    }

    [Export(typeof(IRunSettingsService))]
    [SettingsName(GoogleTestConstants.SettingsName)]
    public class RunSettingsService : IRunSettingsService
    {
        public string Name { get { return GoogleTestConstants.SettingsName; } }

        private IGlobalRunSettings globalRunSettings;

        [ImportingConstructor]
        RunSettingsService([Import(typeof(IGlobalRunSettings))] IGlobalRunSettings globalRunSettings)
        {
            this.globalRunSettings = globalRunSettings;
        }

        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, ILogger log)
        {
            var navigator = inputRunSettingDocument.CreateNavigator();
            if (navigator.MoveToChild("RunSettings", ""))
            {
                if (navigator.MoveToChild(GoogleTestConstants.SettingsName, ""))
                {
                    navigator.DeleteSelf();
                }

                navigator.AppendChild(globalRunSettings.RunSettings.ToXml().CreateNavigator());
            }

            navigator.MoveToRoot();
            return navigator;
        }
    }

    [Export(typeof(ISettingsProvider))]
    [SettingsName(GoogleTestConstants.SettingsName)]
    public class RunSettingsProvider : ISettingsProvider
    {
        public RunSettings Settings { get; private set; }

        public string Name { get; private set; } = GoogleTestConstants.SettingsName;

        public void Load(XmlReader reader)
        {
            Settings = RunSettings.LoadFromXml(reader);
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

    public abstract class AbstractOptions
    {
        public abstract bool PrintTestOutput { get; }
        public abstract string TestDiscoveryRegex { get; }
        public abstract bool RunDisabledTests { get; }
        public abstract int NrOfTestRepetitions { get; }
        public abstract bool ShuffleTests { get; }
        public abstract int ShuffleTestsSeed { get; }
        public abstract List<RegexTraitPair> TraitsRegexesBefore { get; }
        public abstract List<RegexTraitPair> TraitsRegexesAfter { get; }
        public abstract bool UserDebugMode { get; }

        public abstract bool ParallelTestExecution { get; }
        public abstract int MaxNrOfThreads { get; }
        public abstract string BatchForTestSetup { get; }
        public abstract string BatchForTestTeardown { get; }
        public abstract string AdditionalTestExecutionParam { get; }

        public abstract int ReportWaitPeriod { get; }

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

    }

    public class Options : AbstractOptions
    {
        private IXmlOptions XmlOptions { get; }
        private TestEnvironment TestEnvironment { get; }
        private RegexTraitParser RegexTraitParser { get; }


        public Options(IXmlOptions xmlOptions, IMessageLogger logger)
        {
            this.XmlOptions = xmlOptions;
            this.TestEnvironment = new TestEnvironment(this, logger);
            this.RegexTraitParser = new RegexTraitParser(TestEnvironment);
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

        public override bool PrintTestOutput => XmlOptions.PrintTestOutput;


        public const string OptionTestDiscoveryRegex = "Regex for test discovery";
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public const string OptionTestDiscoveryRegexDescription =
            "If non-empty, this regex will be used to discover the Google Test executables containing your tests.\nDefault regex: "
            + GoogleTestDiscoverer.TestFinderRegex;

        public override string TestDiscoveryRegex => XmlOptions.TestDiscoveryRegex;


        public const string OptionRunDisabledTests = "Also run disabled tests";
        public const bool OptionRunDisabledTestsDefaultValue = false;
        public const string OptionRunDisabledTestsDescription =
            "If true, all (selected) tests will be run, even if they have been disabled.\n"
            + "Google Test option:" + GoogleTestConstants.AlsoRunDisabledTestsOption;

        public override bool RunDisabledTests => XmlOptions.RunDisabledTests;


        public const string OptionNrOfTestRepetitions = "Number of test repetitions";
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;
        public const string OptionNrOfTestRepetitionsDescription =
            "Tests will be run for the selected number of times (-1: infinite).\n"
            + "Google Test option:" + GoogleTestConstants.NrOfRepetitionsOption;

        public override int NrOfTestRepetitions
        {
            get
            {
                int nrOfRepetitions = XmlOptions.NrOfTestRepetitions;
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

        public override bool ShuffleTests => XmlOptions.ShuffleTests;


        public const string OptionShuffleTestsSeed = "Shuffle tests: Seed";
        public const int OptionShuffleTestsSeedDefaultValue = GoogleTestConstants.ShuffleTestsSeedDefaultValue;
        public const string OptionShuffleTestsSeedDescription = "0: Seed is computed from system time, 1<n<"
                                                           + GoogleTestConstants.ShuffleTestsSeedMaxValueAsString
                                                           + ": The given seed is used. See note of option '"
                                                           + OptionShuffleTests
                                                           + "'.";

        public override int ShuffleTestsSeed
        {
            get
            {
                int seed = XmlOptions.ShuffleTestsSeed;
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

        public override List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = XmlOptions.TraitsRegexesBefore;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }

        public const string OptionTraitsRegexesAfter = "Regex for setting test traits after test execution";

        public override List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = XmlOptions.TraitsRegexesAfter;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }


        public const string OptionUserDebugMode = "Debug mode";
        public const bool OptionUserDebugModeDefaultValue = false;
        public const string OptionUserDebugModeDescription =
            "If true, debug output will be printed to the test console.";

        public override bool UserDebugMode => XmlOptions.UserDebugMode;


        public const string OptionAdditionalTestExecutionParams = "Additional test execution parameters";
        public const string OptionAdditionalTestExecutionParamsDefaultValue = "";
        public const string OptionAdditionalTestExecutionParamsDescription =
            "Additional parameters for Google Test executable. Placeholders:\n"
            + DescriptionOfPlaceholders;

        public override string AdditionalTestExecutionParam => XmlOptions.AdditionalTestExecutionParam;

        #endregion

        #region ParallelizationOptionsPage

        public const string OptionEnableParallelTestExecution = "Enable parallel test execution";
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;
        public const string OptionEnableParallelTestExecutionDescription =
            "Parallel test execution is achieved by means of different threads, each of which is assigned a number of tests to be executed. The threads will then sequentially invoke the necessary executables to produce the according test results.";

        public override bool ParallelTestExecution => XmlOptions.ParallelTestExecution;


        public const string OptionMaxNrOfThreads = "Maximum number of threads";
        public const int OptionMaxNrOfThreadsDefaultValue = 0;
        public const string OptionMaxNrOfThreadsDescription =
            "Maximum number of threads to be used for test execution (0: all available threads).";

        public override int MaxNrOfThreads
        {
            get
            {
                int result = XmlOptions.MaxNrOfThreads;
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

        public override string BatchForTestSetup => XmlOptions.BatchForTestSetup;


        public const string OptionBatchForTestTeardown = "Test teardown batch file";
        public const string OptionBatchForTestTeardownDefaultValue = "";
        public const string OptionBatchForTestTeardownDescription =
            "Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n"
            + DescriptionOfPlaceholders;

        public override string BatchForTestTeardown => XmlOptions.BatchForTestTeardown;

        #endregion

        #region AdvancedOptionsPage

        public const string OptionReportWaitPeriod = "Wait period during result reporting";
        public const int OptionReportWaitPeriodDefaultValue = 0;
        public const string OptionReportWaitPeriodDescription =
            "Sometimes, not all TestResults are recognized by VS. This is probably due to inter process communication - if anybody has a clean solution for this, please provide a patch. Until then, use this option to ovetcome such problems.\n" +
            "During test reporting, 0: do not pause at all, n: pause for 1ms every nth test (the higher, the faster; 1 is slowest)";

        public override int ReportWaitPeriod
        {
            get
            {
                int period = XmlOptions.ReportWaitPeriod;
                if (period < 0)
                {
                    period = OptionReportWaitPeriodDefaultValue;
                }
                return period;
            }
        }

        #endregion

    }

}