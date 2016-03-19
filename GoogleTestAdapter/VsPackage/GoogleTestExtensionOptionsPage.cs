using System;
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
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class GoogleTestExtensionOptionsPage : Package
    {
        public const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";

        private IGlobalRunSettingsInternal globalRunSettings;
        private GeneralOptionsDialogPage generalOptions;
        private ParallelizationOptionsDialogPage parallelizationOptions;
        private GoogleTestOptionsDialogPage googleTestOptions;


        protected override void Initialize()
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
            SwitchBreakOnFailureOptionCommand.Initialize(this);
            SwitchParallelExecutionOptionCommand.Initialize(this);
        }

        internal bool CatchExtensions
        {
            get { return googleTestOptions.CatchExceptions; }
            set
            {
                googleTestOptions.CatchExceptions = value;
                RefreshVsUi();
            }
        }

        internal bool BreakOnFailure
        {
            get { return googleTestOptions.BreakOnFailure; }
            set
            {
                googleTestOptions.BreakOnFailure = value;
                RefreshVsUi();
            }
        }

        internal bool ParallelTestExecution
        {
            get { return parallelizationOptions.EnableParallelTestExecution; }
            set
            {
                parallelizationOptions.EnableParallelTestExecution = value;
                RefreshVsUi();
            }
        }

        private void RefreshVsUi()
        {
            var vsShell = (IVsUIShell)GetService(typeof(IVsUIShell));
            if (vsShell != null)
            {
                int hr = vsShell.UpdateCommandUI(Convert.ToInt32(false));
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        private void OptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();
        }

        private RunSettings GetRunSettingsFromOptionPages()
        {
            return new RunSettings
            {
                PrintTestOutput = generalOptions.PrintTestOutput,
                TestDiscoveryRegex = generalOptions.TestDiscoveryRegex,
                PathExtension = generalOptions.PathExtension,
                TraitsRegexesBefore = generalOptions.TraitsRegexesBefore,
                TraitsRegexesAfter = generalOptions.TraitsRegexesAfter,
                TestNameSeparator = generalOptions.TestNameSeparator,
                DebugMode = generalOptions.DebugMode,
                AdditionalTestExecutionParam = generalOptions.AdditionalTestExecutionParams,
                BatchForTestSetup = generalOptions.BatchForTestSetup,
                BatchForTestTeardown = generalOptions.BatchForTestTeardown,

                CatchExceptions = googleTestOptions.CatchExceptions,
                BreakOnFailure = googleTestOptions.BreakOnFailure,
                RunDisabledTests = googleTestOptions.RunDisabledTests,
                NrOfTestRepetitions = googleTestOptions.NrOfTestRepetitions,
                ShuffleTests = googleTestOptions.ShuffleTests,
                ShuffleTestsSeed = googleTestOptions.ShuffleTestsSeed,

                ParallelTestExecution = parallelizationOptions.EnableParallelTestExecution,
                MaxNrOfThreads = parallelizationOptions.MaxNrOfThreads
            };
        }

    }

}