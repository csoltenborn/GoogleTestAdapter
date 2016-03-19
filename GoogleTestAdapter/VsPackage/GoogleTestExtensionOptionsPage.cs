using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using GoogleTestAdapter.VsPackage.OptionsPages;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.VsPackage.Commands;
using Microsoft.VisualStudio;

namespace GoogleTestAdapter.VsPackage
{

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), Options.CategoryName, Options.PageGeneralName, 0, 0, true)]
    [ProvideOptionPage(typeof(ParallelizationOptionsDialogPage), Options.CategoryName, Options.PageParallelizationName, 0, 0, true)]
    [ProvideOptionPage(typeof(GoogleTestOptionsDialogPage), Options.CategoryName, Options.PageGoogleTestName, 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class GoogleTestExtensionOptionsPage : Package
    {
        public const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";

        private IGlobalRunSettingsInternal globalRunSettings;
        private GeneralOptionsDialogPage generalOptions;
        private ParallelizationOptionsDialogPage parallelizationOptions;
        private GoogleTestOptionsDialogPage googleTestOptions;


        override protected void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

            generalOptions = (GeneralOptionsDialogPage)GetDialogPage(typeof(GeneralOptionsDialogPage));
            parallelizationOptions = (ParallelizationOptionsDialogPage)GetDialogPage(typeof(ParallelizationOptionsDialogPage));
            googleTestOptions = (GoogleTestOptionsDialogPage)GetDialogPage(typeof(GoogleTestOptionsDialogPage));

            globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();

            generalOptions.PropertyChanged += OptionsChanged;
            parallelizationOptions.PropertyChanged += OptionsChanged;
            googleTestOptions.PropertyChanged += OptionsChanged;

            SwitchCatchExceptionsOptionCommand.Initialize(this);
        }

        internal bool CatchExtensions {
            get { return googleTestOptions.CatchExceptions; }
            set
            {
                googleTestOptions.CatchExceptions = value;
                var vsShell = (IVsUIShell)GetService(typeof(IVsUIShell));
                if (vsShell != null)
                {
                    int hr = vsShell.UpdateCommandUI(0);
                    ErrorHandler.ThrowOnFailure(hr);
                }
            }
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
            runSettings.PathExtension = generalOptions.PathExtension;
            runSettings.TraitsRegexesBefore = generalOptions.TraitsRegexesBefore;
            runSettings.TraitsRegexesAfter = generalOptions.TraitsRegexesAfter;
            runSettings.TestNameSeparator = generalOptions.TestNameSeparator;
            runSettings.DebugMode = generalOptions.DebugMode;
            runSettings.AdditionalTestExecutionParam = generalOptions.AdditionalTestExecutionParams;
            runSettings.BatchForTestSetup = generalOptions.BatchForTestSetup;
            runSettings.BatchForTestTeardown = generalOptions.BatchForTestTeardown;

            runSettings.CatchExceptions = googleTestOptions.CatchExceptions;
            runSettings.BreakOnFailure = googleTestOptions.BreakOnFailure;
            runSettings.RunDisabledTests = googleTestOptions.RunDisabledTests;
            runSettings.NrOfTestRepetitions = googleTestOptions.NrOfTestRepetitions;
            runSettings.ShuffleTests = googleTestOptions.ShuffleTests;
            runSettings.ShuffleTestsSeed = googleTestOptions.ShuffleTestsSeed;

            runSettings.ParallelTestExecution = parallelizationOptions.EnableParallelTestExecution;
            runSettings.MaxNrOfThreads = parallelizationOptions.MaxNrOfThreads;

            return runSettings;
        }
    }

}
