using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using GoogleTestAdapter;

namespace GoogleTestAdapterVSIX
{

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), Options.CategoryName, Options.PageGeneralName, 0, 0, true)]
    [ProvideOptionPage(typeof(ParallelizationOptionsDialogPage), Options.CategoryName, Options.PageParallelizationName, 0, 0, true)]
    [ProvideOptionPage(typeof(AdvancedOptionsDialogPage), Options.CategoryName, Options.PageAdvancedName, 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class GoogleTestExtensionOptionsPage : Package
    {
        public const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";

        override protected void Initialize()
        {
            base.Initialize();

            DialogPage page = GetDialogPage(typeof(GeneralOptionsDialogPage));
            page.SaveSettingsToStorage();
            page = GetDialogPage(typeof(ParallelizationOptionsDialogPage));
            page.SaveSettingsToStorage();
            page = GetDialogPage(typeof(AdvancedOptionsDialogPage));
            page.SaveSettingsToStorage();
        }

    }

    public class GeneralOptionsDialogPage : DialogPage
    {
        private const string TraitsDescription = "Allows to override/add traits for testcases matching a regex. Traits are build up in 3 phases: 1st, traits are assigned to tests according to the 'Traits before' option. 2nd, the tests' traits (defined via the macros in GTA_Traits.h) are added to the tests, overriding traits from phase 1 with new values. 3rd, the 'Traits after' option is evaluated, again in an overriding manner.\nSyntax: "
                                                 + Options.TraitsRegexesRegexSeparator +
                                                 " separates the regex from the traits, the trait's name and value are separated by "
                                                 + Options.TraitsRegexesTraitSeparator +
                                                 " and each pair of regex and trait is separated by "
                                                 + Options.TraitsRegexesPairSeparator + ".\nExample: " +
                                                 @"MySuite\.*"
                                                 + Options.TraitsRegexesRegexSeparator + "Type"
                                                 + Options.TraitsRegexesTraitSeparator + "Small"
                                                 + Options.TraitsRegexesPairSeparator +
                                                 @"MySuite2\.*|MySuite3\.*"
                                                 + Options.TraitsRegexesRegexSeparator + "Type"
                                                 + Options.TraitsRegexesTraitSeparator + "Medium";

        private const string ShuffleTestsSeedDescription = "0: Seed is computed from system time, 1<n<"
            + GoogleTestConstants.ShuffleTestsSeedMaxValueAsString
            + ": The given seed is used. See note of option '"
            + Options.OptionShuffleTests
            + "'";


        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionPrintTestOutput)]
        [Description("Print the output of the Google Test executable(s) to the Tests Output window.")]
        public bool PrintTestOutput { get; set; } = Options.OptionPrintTestOutputDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestDiscoveryRegex)]
        [Description("If non-empty, this regex will be used to discover the Google Test executables containing your tests.\nDefault regex: " + GoogleTestDiscoverer.TestFinderRegex)]
        public string TestDiscoveryRegex { get; set; } = Options.OptionTestDiscoveryRegexDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionRunDisabledTests)]
        [Description("If true, all (selected) tests will be run, even if they have been disabled.\nGoogle Test option:" + GoogleTestConstants.AlsoRunDisabledTestsOption)]
        public bool RunDisabledTests { get; set; } = Options.OptionRunDisabledTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionNrOfTestRepetitions)]
        [Description("Tests will be run for the selected number of times (-1: infinite).\nGoogle Test option:" + GoogleTestConstants.NrOfRepetitionsOption)]
        public int NrOfTestRepetitions { get; set; } = Options.OptionNrOfTestRepetitionsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTests)]
        [Description("If true, tests will be executed in random order. Note that a true randomized order is only given when executing all tests in non-parallel fashion. Otherwise, the test excutables will most likely be executed more than once - random order is than restricted to the according executions.\nGoogle Test option:" + GoogleTestConstants.ShuffleTestsOption)]
        public bool ShuffleTests { get; set; } = Options.OptionShuffleTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTestsSeed)]
        [Description(ShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed { get; set; } = Options.OptionShuffleTestsSeedDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionUserDebugMode)]
        [Description("If true, debug output will be printed to the test console.")]
        public bool UserDebugMode { get; set; } = Options.OptionUserDebugModeDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesBefore)]
        [Description(TraitsDescription)]
        public string TraitsRegexesBefore { get; set; } = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesAfter)]
        [Description(TraitsDescription)]
        public string TraitsRegexesAfter { get; set; } = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionAdditionalTestExecutionParam)]
        [Description("Additional parameters for Google Test executable. Placeholders:\n" + Options.DescriptionOfPlaceholders)]
        public string AdditionalTestExecutionParams { get; set; } = Options.OptionAdditionalTestExecutionParamDefaultValue;

    }

    public class ParallelizationOptionsDialogPage : DialogPage
    {

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionEnableParallelTestExecution)]
        [Description("Parallel test execution is achieved by means of different threads, each of which is assigned a number of tests to be executed. The threads will then sequentially invoke the necessary executables to produce the according test results.")]
        public bool EnableParallelTestExecution { get; set; } = Options.OptionPrintTestOutputDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionMaxNrOfThreads)]
        [Description("Maximum number of threads to be used for test execution (0: all available threads).")]
        public int MaxNrOfThreads { get; set; } = Options.OptionMaxNrOfThreadsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestSetupBatch)]
        [Description("Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n" + Options.DescriptionOfPlaceholders)]
        public string BatchForTestSetup { get; set; } = Options.OptionTestSetupBatchDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestTeardownBatch)]
        [Description("Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n" + Options.DescriptionOfPlaceholders)]
        public string BatchForTestTeardown { get; set; } = Options.OptionTestTeardownBatchDefaultValue;

    }

    public class AdvancedOptionsDialogPage : DialogPage
    {

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionReportWaitPeriod)]
        [Description("Sometimes, not all TestResults are recognized by VS. This is probably due to inter process communication - if anybody has a clean solution for this, please provide a patch. Until then, use this option to ovetcome such problems.\n" +
            "During test reporting, 0: do not pause at all, n: pause for 1ms every nth test (the higher, the faster; 1 is slowest)")]
        public int ReportWaitPeriod { get; set; } = Options.OptionReportWaitPeriodDefaultValue;

    }

}