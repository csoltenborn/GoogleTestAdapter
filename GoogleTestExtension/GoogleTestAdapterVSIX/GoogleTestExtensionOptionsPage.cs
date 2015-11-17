using GoogleTestAdapter;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace GoogleTestAdapterVSIX
{
    internal interface IGlobalRunSettingsInternal : IGlobalRunSettings
    {
        new RunSettings RunSettings { get; set; }
    }

    [Export(typeof(IGlobalRunSettings))]
    [Export(typeof(IGlobalRunSettingsInternal))]
    public class GlobalRunSettingsProvider : IGlobalRunSettingsInternal
    {
        public RunSettings RunSettings { get; set; } = new RunSettings();
    }

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

        private IGlobalRunSettingsInternal globalRunSettings;
        private GeneralOptionsDialogPage generalOptions;
        private ParallelizationOptionsDialogPage parallelizationOptions;
        private AdvancedOptionsDialogPage advancedOptions;


        override protected void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

            generalOptions = (GeneralOptionsDialogPage)GetDialogPage(typeof(GeneralOptionsDialogPage));
            parallelizationOptions = (ParallelizationOptionsDialogPage)GetDialogPage(typeof(ParallelizationOptionsDialogPage));
            advancedOptions = (AdvancedOptionsDialogPage)GetDialogPage(typeof(AdvancedOptionsDialogPage));

            globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();

            generalOptions.PropertyChanged += OptionsChanged;
            parallelizationOptions.PropertyChanged += OptionsChanged;
            advancedOptions.PropertyChanged += OptionsChanged;
        }

        private void OptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();
        }

        private RunSettings GetRunSettingsFromOptionPages()
        {
            RunSettings runSettings = new RunSettings();

            runSettings.PrintTestOutput = generalOptions.PrintTestOutput;
            runSettings.TestDiscoveryRegex = generalOptions.TestDiscoveryRegex;
            runSettings.RunDisabledTests = generalOptions.RunDisabledTests;
            runSettings.NrOfTestRepetitions = generalOptions.NrOfTestRepetitions;
            runSettings.ShuffleTests = generalOptions.ShuffleTests;
            runSettings.ShuffleTestsSeed = generalOptions.ShuffleTestsSeed;
            runSettings.TraitsRegexesBefore = generalOptions.TraitsRegexesBefore;
            runSettings.TraitsRegexesAfter = generalOptions.TraitsRegexesAfter;
            runSettings.DebugMode = generalOptions.DebugMode;
            runSettings.AdditionalTestExecutionParam = generalOptions.AdditionalTestExecutionParams;

            runSettings.ParallelTestExecution = parallelizationOptions.EnableParallelTestExecution;
            runSettings.MaxNrOfThreads = parallelizationOptions.MaxNrOfThreads;
            runSettings.BatchForTestSetup = parallelizationOptions.BatchForTestSetup;
            runSettings.BatchForTestTeardown = parallelizationOptions.BatchForTestTeardown;

            runSettings.ReportWaitPeriod = advancedOptions.ReportWaitPeriod;
            runSettings.DevelopmentMode = advancedOptions.DevelopmentMode;

            return runSettings;
        }
    }


    public class NotifyingDialogPage : DialogPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GeneralOptionsDialogPage : NotifyingDialogPage
    {


        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionPrintTestOutput)]
        [Description(Options.OptionPrintTestOutputDescription)]
        public bool PrintTestOutput
        {
            get { return printTestOutput; }
            set { SetAndNotify(ref printTestOutput, value); }
        }
        private bool printTestOutput = Options.OptionPrintTestOutputDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestDiscoveryRegex)]
        [Description(Options.OptionTestDiscoveryRegexDescription)]
        public string TestDiscoveryRegex
        {
            get { return testDiscoveryRegex; }
            set { SetAndNotify(ref testDiscoveryRegex, value); }
        }
        private string testDiscoveryRegex = Options.OptionTestDiscoveryRegexDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionRunDisabledTests)]
        [Description(Options.OptionRunDisabledTestsDescription)]
        public bool RunDisabledTests
        {
            get { return runDisabledTests; }
            set { SetAndNotify(ref runDisabledTests, value); }
        }
        private bool runDisabledTests = Options.OptionRunDisabledTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionNrOfTestRepetitions)]
        [Description(Options.OptionNrOfTestRepetitionsDescription)]
        public int NrOfTestRepetitions
        {
            get { return nrOfTestRepetitions; }
            set { SetAndNotify(ref nrOfTestRepetitions, value); }
        }
        private int nrOfTestRepetitions = Options.OptionNrOfTestRepetitionsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTests)]
        [Description(Options.OptionShuffleTestsDescription)]
        public bool ShuffleTests
        {
            get { return shuffleTests; }
            set { SetAndNotify(ref shuffleTests, value); }
        }
        private bool shuffleTests = Options.OptionShuffleTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTestsSeed)]
        [Description(Options.OptionShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed
        {
            get { return shuffleTestsSeed; }
            set { SetAndNotify(ref shuffleTestsSeed, value); }
        }
        private int shuffleTestsSeed = Options.OptionShuffleTestsSeedDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionDebugMode)]
        [Description(Options.OptionDebugModeDescription)]
        public bool DebugMode
        {
            get { return debugMode; }
            set { SetAndNotify(ref debugMode, value); }
        }
        private bool debugMode = Options.OptionDebugModeDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesBefore)]
        [Description(Options.OptionTraitsDescription)]
        public string TraitsRegexesBefore
        {
            get { return traitsRegexesBefore; }
            set { SetAndNotify(ref traitsRegexesBefore, value); }
        }
        private string traitsRegexesBefore = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesAfter)]
        [Description(Options.OptionTraitsDescription)]
        public string TraitsRegexesAfter
        {
            get { return traitsRegexesAfter; }
            set { SetAndNotify(ref traitsRegexesAfter, value); }
        }
        private string traitsRegexesAfter = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionAdditionalTestExecutionParams)]
        [Description(Options.OptionAdditionalTestExecutionParamsDescription)]
        public string AdditionalTestExecutionParams
        {
            get { return additionalTestExecutionParams; }
            set { SetAndNotify(ref additionalTestExecutionParams, value); }
        }
        private string additionalTestExecutionParams = Options.OptionAdditionalTestExecutionParamsDefaultValue;
    }

    public class ParallelizationOptionsDialogPage : NotifyingDialogPage
    {

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionEnableParallelTestExecution)]
        [Description(Options.OptionEnableParallelTestExecutionDescription)]
        public bool EnableParallelTestExecution
        {
            get { return enableParallelTestExecution; }
            set { SetAndNotify(ref enableParallelTestExecution, value); }
        }
        private bool enableParallelTestExecution = Options.OptionEnableParallelTestExecutionDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionMaxNrOfThreads)]
        [Description(Options.OptionMaxNrOfThreadsDescription)]
        public int MaxNrOfThreads
        {
            get { return maxNrOfThreads; }
            set { SetAndNotify(ref maxNrOfThreads, value); }
        }
        private int maxNrOfThreads = Options.OptionMaxNrOfThreadsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionBatchForTestSetup)]
        [Description(Options.OptionBatchForTestSetupDescription)]
        public string BatchForTestSetup
        {
            get { return batchForTestSetup; }
            set { SetAndNotify(ref batchForTestSetup, value); }
        }
        private string batchForTestSetup = Options.OptionBatchForTestSetupDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionBatchForTestTeardown)]
        [Description(Options.OptionBatchForTestTeardownDescription)]
        public string BatchForTestTeardown
        {
            get { return batchForTestTeardown; }
            set { SetAndNotify(ref batchForTestTeardown, value); }
        }
        private string batchForTestTeardown = Options.OptionBatchForTestTeardownDefaultValue;
    }

    public class AdvancedOptionsDialogPage : NotifyingDialogPage
    {
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionReportWaitPeriod)]
        [Description(Options.OptionReportWaitPeriodDescription)]
        public int ReportWaitPeriod
        {
            get { return reportWaitPeriod; }
            set { SetAndNotify(ref reportWaitPeriod, value); }
        }
        private int reportWaitPeriod = Options.OptionReportWaitPeriodDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionDevelopmentMode)]
        [Description(Options.OptionDevelopmentModeDescription)]
        public bool DevelopmentMode
        {
            get { return developmentMode; }
            set { SetAndNotify(ref developmentMode, value); }
        }
        private bool developmentMode = Options.OptionDevelopmentModeDefaultValue;
    }
}
