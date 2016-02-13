using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using GoogleTestAdapter.VsPackage.OptionsPages;
using GoogleTestAdapter.TestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage
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

            return runSettings;
        }
    }

}
