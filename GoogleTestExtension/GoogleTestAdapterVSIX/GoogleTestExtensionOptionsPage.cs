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
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionPrintTestOutput)]
        [Description(Options.OptionPrintTestOutputDescription)]
        public bool PrintTestOutput { get; set; } = Options.OptionPrintTestOutputDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestDiscoveryRegex)]
        [Description(Options.OptionTestDiscoveryRegexDescription)]
        public string TestDiscoveryRegex { get; set; } = Options.OptionTestDiscoveryRegexDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionRunDisabledTests)]
        [Description(Options.OptionRunDisabledTestsDescription)]
        public bool RunDisabledTests { get; set; } = Options.OptionRunDisabledTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionNrOfTestRepetitions)]
        [Description(Options.OptionNrOfTestRepetitionsDescription)]
        public int NrOfTestRepetitions { get; set; } = Options.OptionNrOfTestRepetitionsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTests)]
        [Description(Options.OptionShuffleTestsDescription)]
        public bool ShuffleTests { get; set; } = Options.OptionShuffleTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTestsSeed)]
        [Description(Options.OptionShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed { get; set; } = Options.OptionShuffleTestsSeedDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionDebugMode)]
        [Description(Options.OptionDebugModeDescription)]
        public bool DebugMode { get; set; } = Options.OptionDebugModeDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesBefore)]
        [Description(Options.OptionTraitsDescription)]
        public string TraitsRegexesBefore { get; set; } = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesAfter)]
        [Description(Options.OptionTraitsDescription)]
        public string TraitsRegexesAfter { get; set; } = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionAdditionalTestExecutionParams)]
        [Description(Options.OptionAdditionalTestExecutionParamsDescription)]
        public string AdditionalTestExecutionParams { get; set; } = Options.OptionAdditionalTestExecutionParamDefaultValue;
    }

    public class ParallelizationOptionsDialogPage : DialogPage
    {
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionEnableParallelTestExecution)]
        [Description(Options.OptionEnableParallelTestExecutionDescription)]
        public bool EnableParallelTestExecution { get; set; } = Options.OptionPrintTestOutputDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionMaxNrOfThreads)]
        [Description(Options.OptionMaxNrOfThreadsDescription)]
        public int MaxNrOfThreads { get; set; } = Options.OptionMaxNrOfThreadsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestSetupBatch)]
        [Description(Options.OptionTestSetupBatchDescription)]
        public string BatchForTestSetup { get; set; } = Options.OptionTestSetupBatchDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestTeardownBatch)]
        [Description(Options.OptionTestTeardownBatchDescription)]
        public string BatchForTestTeardown { get; set; } = Options.OptionTestTeardownBatchDefaultValue;
    }

    public class AdvancedOptionsDialogPage : DialogPage
    {
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionReportWaitPeriod)]
        [Description(Options.OptionReportWaitPeriodDescription)]
        public int ReportWaitPeriod { get; set; } = Options.OptionReportWaitPeriodDefaultValue;
    }

}