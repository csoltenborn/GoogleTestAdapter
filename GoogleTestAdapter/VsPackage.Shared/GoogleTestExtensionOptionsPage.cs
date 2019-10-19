// This file has been modified by Microsoft on 7/2017.

using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.VsPackage.Commands;
using GoogleTestAdapter.VsPackage.Debugging;
using GoogleTestAdapter.VsPackage.Helpers;
using GoogleTestAdapter.VsPackage.OptionsPages;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel;
using EnvDTE;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.VsPackage.GTA.Helpers;
using Microsoft.VisualStudio.AsyncPackageHelpers;
using VsPackage.Shared.Settings;
using TestDiscoveryOptionsDialogPage = GoogleTestAdapter.VsPackage.OptionsPages.TestDiscoveryOptionsDialogPage;

namespace GoogleTestAdapter.VsPackage
{

    [AsyncPackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Microsoft.VisualStudio.AsyncPackageHelpers.ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageGeneralName, 0, 0, true)]
    [ProvideOptionPage(typeof(TestDiscoveryOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageTestDiscovery, 0, 0, true)]
    [ProvideOptionPage(typeof(TestExecutionOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageTestExecution, 0, 0, true)]
    [ProvideOptionPage(typeof(GoogleTestOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageGoogleTestName, 0, 0, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed partial class GoogleTestExtensionOptionsPage : Package, IGoogleTestExtensionOptionsPage, IAsyncLoadablePackageInitialize, IDisposable
    {
        private const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";

        private readonly string _debuggingNamedPipeId = Guid.NewGuid().ToString();

        private IGlobalRunSettingsInternal _globalRunSettings;

        private GeneralOptionsDialogPage _generalOptions;
        private TestDiscoveryOptionsDialogPage _testDiscoveryOptions;
        private TestExecutionOptionsDialogPage _testExecutionOptions;
        private GoogleTestOptionsDialogPage _googleTestOptions;

        private DebuggerAttacherServiceHost _debuggerAttacherServiceHost;

        private bool _isAsyncLoadSupported;

        protected override void Initialize()
        {
            base.Initialize();

            _isAsyncLoadSupported = this.IsAsyncPackageSupported();
            if (!_isAsyncLoadSupported)
            {
                var componentModel = (IComponentModel) GetGlobalService(typeof(SComponentModel));
                _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

                VsSettingsStorage.Init(this);

                DoInitialize();
            }
        }

        IVsTask IAsyncLoadablePackageInitialize.Initialize(IAsyncServiceProvider serviceProvider, IProfferAsyncService profferService,
            IAsyncProgressCallback progressCallback)
        {
            if (!_isAsyncLoadSupported)
            {
                throw new InvalidOperationException("Async Initialize method should not be called when async load is not supported.");
            }

            return ThreadHelper.JoinableTaskFactory.RunAsync<object>(async () =>
            {
                var componentModel = await serviceProvider.GetServiceAsync<IComponentModel>(typeof(SComponentModel));
                _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

                VsSettingsStorage.Init(this);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                DoInitialize();

                return null;
            }).AsVsTask();
        }

        private void DoInitialize()
        {
            InitializeOptions();
            InitializeCommands();
            InitializeDebuggerAttacherService();
            DisplayReleaseNotesIfNecessary();
        }

        private void InitializeOptions()
        {
            _generalOptions = (GeneralOptionsDialogPage) GetDialogPage(typeof(GeneralOptionsDialogPage));
            _testDiscoveryOptions =
                (TestDiscoveryOptionsDialogPage) GetDialogPage(typeof(TestDiscoveryOptionsDialogPage));
            _testExecutionOptions =
                (TestExecutionOptionsDialogPage) GetDialogPage(typeof(TestExecutionOptionsDialogPage));
            _googleTestOptions = (GoogleTestOptionsDialogPage) GetDialogPage(typeof(GoogleTestOptionsDialogPage));

            var optionsUpdater = new OptionsUpdater(_testDiscoveryOptions, _testExecutionOptions, _generalOptions, new ActivityLogLogger(this, () => OutputMode.Verbose));
            if (optionsUpdater.UpdateIfNecessary())
            {
                _testDiscoveryOptions.SaveSettingsToStorage();
                _testExecutionOptions.SaveSettingsToStorage();
                _generalOptions.SaveSettingsToStorage();
            }

            _globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();

            _generalOptions.PropertyChanged += OptionsChanged;
            _testDiscoveryOptions.PropertyChanged += OptionsChanged;
            _testExecutionOptions.PropertyChanged += OptionsChanged;
            _googleTestOptions.PropertyChanged += OptionsChanged;
        }

        private void InitializeCommands()
        {
            SwitchCatchExceptionsOptionCommand.Initialize(this);
            SwitchBreakOnFailureOptionCommand.Initialize(this);
            SwitchParallelExecutionOptionCommand.Initialize(this);
            SwitchPrintTestOutputOptionCommand.Initialize(this);
        }

        private void InitializeDebuggerAttacherService()
        {
            var logger = new ActivityLogLogger(this, () => _generalOptions.OutputMode);
            var debuggerAttacher = new VsDebuggerAttacher(this);
            _debuggerAttacherServiceHost = new DebuggerAttacherServiceHost(_debuggingNamedPipeId, debuggerAttacher, logger);
            try
            {
                _debuggerAttacherServiceHost.Open();
            }
            catch (CommunicationException)
            {
                _debuggerAttacherServiceHost.Abort();
                _debuggerAttacherServiceHost = null;
            }
        }

        public IVsActivityLog GetActivityLog()
        {
            return GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }


        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", 
            MessageId = nameof(_debuggerAttacherServiceHost), Justification = "Close() includes Dispose()")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _generalOptions?.Dispose();
                _testDiscoveryOptions?.Dispose();
                _testExecutionOptions?.Dispose();
                _googleTestOptions?.Dispose();

                try
                {
                    _debuggerAttacherServiceHost?.Close();
                }
                catch (CommunicationException)
                {
                    _debuggerAttacherServiceHost?.Abort();
                }
            }
            base.Dispose(disposing);
        }

        public bool CatchExtensions
        {
            get { return _googleTestOptions.CatchExceptions; }
            set
            {
                _googleTestOptions.CatchExceptions = value;
                RefreshVsUi();
            }
        }

        public bool BreakOnFailure
        {
            get { return _googleTestOptions.BreakOnFailure; }
            set
            {
                _googleTestOptions.BreakOnFailure = value;
                RefreshVsUi();
            }
        }

        public bool PrintTestOutput
        {
            get { return _generalOptions.PrintTestOutput; }
            set
            {
                _generalOptions.PrintTestOutput = value;
                RefreshVsUi();
            }
        }

        public bool ParallelTestExecution
        {
            get { return _testExecutionOptions.EnableParallelTestExecution; }
            set
            {
                _testExecutionOptions.EnableParallelTestExecution = value;
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
            _globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();
        }

        private RunSettings GetRunSettingsFromOptionPages()
        {
            GetVisualStudioConfiguration(out string solutionDir, out string platformName, out string configurationName);

            return new RunSettings
            {
                PrintTestOutput = _generalOptions.PrintTestOutput,
                OutputMode = _generalOptions.OutputMode,
                TimestampMode = _generalOptions.TimestampMode,
                SeverityMode = _generalOptions.SeverityMode,
                SummaryMode = _generalOptions.SummaryMode,
                PrefixOutputWithGta = _generalOptions.PrefixOutputWithGta,
                SkipOriginCheck = _generalOptions.SkipOriginCheck,

                CatchExceptions = _googleTestOptions.CatchExceptions,
                BreakOnFailure = _googleTestOptions.BreakOnFailure,
                RunDisabledTests = _googleTestOptions.RunDisabledTests,
                NrOfTestRepetitions = _googleTestOptions.NrOfTestRepetitions,
                ShuffleTests = _googleTestOptions.ShuffleTests,
                ShuffleTestsSeed = _googleTestOptions.ShuffleTestsSeed,

                TestDiscoveryRegex = _testDiscoveryOptions.TestDiscoveryRegex,
                TestDiscoveryTimeoutInSeconds = _testDiscoveryOptions.TestDiscoveryTimeoutInSeconds,
                TraitsRegexesBefore = _testDiscoveryOptions.TraitsRegexesBefore,
                TraitsRegexesAfter = _testDiscoveryOptions.TraitsRegexesAfter,
                TestNameSeparator = _testDiscoveryOptions.TestNameSeparator,
                ParseSymbolInformation = _testDiscoveryOptions.ParseSymbolInformation,

                AdditionalPdbs = _testExecutionOptions.AdditionalPdbs,
                WorkingDir = _testExecutionOptions.WorkingDir,
                PathExtension = _testExecutionOptions.PathExtension,
                EnvironmentVariables = _testExecutionOptions.EnvironmentVariables,
                AdditionalTestExecutionParam = _testExecutionOptions.AdditionalTestExecutionParams,
                BatchForTestSetup = _testExecutionOptions.BatchForTestSetup,
                BatchForTestTeardown = _testExecutionOptions.BatchForTestTeardown,
                KillProcessesOnCancel = _testExecutionOptions.KillProcessesOnCancel,
                ExitCodeTestCase = _testExecutionOptions.ExitCodeTestCase,
                ParallelTestExecution = _testExecutionOptions.EnableParallelTestExecution,
                MaxNrOfThreads = _testExecutionOptions.MaxNrOfThreads,
                DebuggerKind = _testExecutionOptions.DebuggerKind,
                MissingTestsReportMode = _testExecutionOptions.MissingTestsReportMode,

                DebuggingNamedPipeId = _debuggingNamedPipeId,
                SolutionDir = solutionDir,
                PlatformName = platformName,
                ConfigurationName = configurationName
            };
        }

        private void GetVisualStudioConfiguration(out string solutionDir, out string platformName, out string configurationName)
        {
            var logger = new ActivityLogLogger(this, () => OutputMode.Verbose);

            solutionDir = platformName = configurationName = null;
            try
            {
                if (GetService(typeof(DTE)) is DTE dte)
                {
                    try
                    {
                        solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Exception caught while receiving solution dir from VS instance. dte.Solution.FullName: {dte.Solution.FullName}. Exception:{Environment.NewLine}{e}");
                    }

                    if (dte.Solution.Projects.Count > 0)
                    {  
                        var configurationManager = dte.Solution.Projects.Item(1).ConfigurationManager;  
                        var activeConfiguration = configurationManager.ActiveConfiguration;

                        platformName = activeConfiguration.PlatformName;
                        configurationName = activeConfiguration.ConfigurationName;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Exception while receiving configuration info from Visual Studio.{Environment.NewLine}{e}");
            }
        }
    }

}