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
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), GoogleTestAdapterOptions.CategoryName, GoogleTestAdapterOptions.PageGeneralName, 0, 0, true)]
    [ProvideOptionPage(typeof(ParallelizationOptionsDialogPage), GoogleTestAdapterOptions.CategoryName, GoogleTestAdapterOptions.PageParallelizationName, 0, 0, true)]
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
        }

    }

    public class GeneralOptionsDialogPage : DialogPage
    {
        private const string TraitsDescription = "Allows to override/add traits for testcases matching a regex. Traits are build up in 3 phases: 1st, traits are assigned to tests according to the 'Traits before' option. 2nd, the tests' traits (defined via the macros in GTA_Traits.h) are added to the tests, overriding traits from phase 1 with new values. 3rd, the 'Traits after' option is evaluated, again in an overriding manner.\nSyntax: "
                                                 + GoogleTestAdapterOptions.TraitsRegexesRegexSeparator +
                                                 " separates the regex from the traits, the trait's name and value are separated by "
                                                 + GoogleTestAdapterOptions.TraitsRegexesTraitSeparator +
                                                 " and each pair of regex and trait is separated by "
                                                 + GoogleTestAdapterOptions.TraitsRegexesPairSeparator + ".\nExample: " +
                                                 @"MySuite\.*"
                                                 + GoogleTestAdapterOptions.TraitsRegexesRegexSeparator + "Type"
                                                 + GoogleTestAdapterOptions.TraitsRegexesTraitSeparator + "Small"
                                                 + GoogleTestAdapterOptions.TraitsRegexesPairSeparator +
                                                 @"MySuite2\.*|MySuite3\.*"
                                                 + GoogleTestAdapterOptions.TraitsRegexesRegexSeparator + "Type"
                                                 + GoogleTestAdapterOptions.TraitsRegexesTraitSeparator + "Medium";

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionPrintTestOutput)]
        [Description("Print the output of the Google Test executable(s) to the Tests Output window.")]
        public bool PrintTestOutput { get; set; } = GoogleTestAdapterOptions.OptionPrintTestOutputDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionTestDiscoveryRegex)]
        [Description("If non-empty, this regex will be used to discover the Google Test executables containing your tests.\nDefault regex: " + GoogleTestAdapter.Constants.TestFinderRegex)]
        public string TestDiscoveryRegex { get; set; } = GoogleTestAdapterOptions.OptionTestDiscoveryRegexDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionRunDisabledTests)]
        [Description("If true, all (selected) tests will be run, even if they have been disabled.\nGoogle Test option:" + GoogleTestConstants.AlsoRunDisabledTestsOption)]
        public bool RunDisabledTests { get; set; } = GoogleTestAdapterOptions.OptionRunDisabledTestsDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionNrOfTestRepetitions)]
        [Description("Tests will be run for the selected number of times (-1: infinite).\nGoogle Test option:" + GoogleTestConstants.NrOfRepetitionsOption)]
        public int NrOfTestRepetitions { get; set; } = GoogleTestAdapterOptions.OptionNrOfTestRepetitionsDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionShuffleTests)]
        [Description("If true, tests will be executed in random order. Note that a true randomized order is only given when executing all tests in non-parallel fashion. Otherwise, the test excutables will most likely be executed more than once - random order is than restricted to the according executions.\nGoogle Test option:" + GoogleTestConstants.ShuffleTestsOption)]
        public bool ShuffleTests { get; set; } = GoogleTestAdapterOptions.OptionShuffleTestsDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionUserDebugMode)]
        [Description("If true, debug output will be printed to the test console.")]
        public bool UserDebugMode { get; set; } = GoogleTestAdapterOptions.OptionUserDebugModeDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionTraitsRegexesBefore)]
        [Description(TraitsDescription)]
        public string TraitsRegexesBefore { get; set; } = GoogleTestAdapterOptions.OptionTraitsRegexesDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionTraitsRegexesAfter)]
        [Description(TraitsDescription)]
        public string TraitsRegexesAfter { get; set; } = GoogleTestAdapterOptions.OptionTraitsRegexesDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionAdditionalTestExecutionParam)]
        [Description("Additional parameters for Google Test executable. Placeholders:\n" + GoogleTestAdapterOptions.DescriptionOfPlaceholders)]
        public string AdditionalTestExecutionParams { get; set; } = GoogleTestAdapterOptions.OptionAdditionalTestExecutionParamDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName("Test counter")]
        [Description("Workaround for bug. 0: No pauses at all. n: Pause every nth test (the higher, the faster; 1 is slowest)")]
        public int TestCounter { get; set; } = 1;

    }

    public class ParallelizationOptionsDialogPage : DialogPage
    {

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionEnableParallelTestExecution)]
        [Description("Parallel test execution is achieved by means of different threads, each of which is assigned a number of tests to be executed. The threads will then sequentially invoke the necessary executables to produce the according test results.")]
        public bool EnableParallelTestExecution { get; set; } = GoogleTestAdapterOptions.OptionPrintTestOutputDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionMaxNrOfThreads)]
        [Description("Maximum number of threads to be used for test execution (0: all available threads).")]
        public int MaxNrOfThreads { get; set; } = GoogleTestAdapterOptions.OptionMaxNrOfThreadsDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionTestSetupBatch)]
        [Description("Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n" + GoogleTestAdapterOptions.DescriptionOfPlaceholders)]
        public string BatchForTestSetup { get; set; } = GoogleTestAdapterOptions.OptionTestSetupBatchDefaultValue;

        [Category(GoogleTestAdapterOptions.CategoryName)]
        [DisplayName(GoogleTestAdapterOptions.OptionTestTeardownBatch)]
        [Description("Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n" + GoogleTestAdapterOptions.DescriptionOfPlaceholders)]
        public string BatchForTestTeardown { get; set; } = GoogleTestAdapterOptions.OptionTestTeardownBatchDefaultValue;

    }

}