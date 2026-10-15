// This file has been modified by Microsoft on 7/2017.

using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.VsPackage.Automation;
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
using System.Threading;
using Task = System.Threading.Tasks.Task;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.VsPackage.GTA.Helpers;
using VsPackage.Shared.Settings;
using TestDiscoveryOptionsDialogPage = GoogleTestAdapter.VsPackage.OptionsPages.TestDiscoveryOptionsDialogPage;

namespace GoogleTestAdapter.VsPackage
{

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // the package must also be loaded if VS is started with a solution or folder, since e.g. debugging tests depends on it
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(FolderOpenedUIContextString, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageGeneralName, 0, 0, true)]
    [ProvideOptionPage(typeof(TestDiscoveryOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageTestDiscovery, 0, 0, true)]
    [ProvideOptionPage(typeof(TestExecutionOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageTestExecution, 0, 0, true)]
    [ProvideOptionPage(typeof(GoogleTestOptionsDialogPage), OptionsCategoryName, SettingsWrapper.PageGoogleTestName, 0, 0, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // DTE.GetObject("GoogleTestAdapter"), e.g. for scripts and MCP servers
    [ProvideAutomationObject(GoogleTestAdapterAutomation.Name)]
    public sealed partial class GoogleTestExtensionOptionsPage : AsyncPackage, IGoogleTestExtensionOptionsPage, IDisposable
    {
        private const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";
        // VSConstants.UICONTEXT.FolderOpened_string, which is not available in the referenced VS SDK
        private const string FolderOpenedUIContextString = "4646B819-1AE0-4E79-97F4-8A8176FDD664";

        private readonly string _debuggingNamedPipeId = Guid.NewGuid().ToString();

        private IGlobalRunSettingsInternal _globalRunSettings;

        private GeneralOptionsDialogPage _generalOptions;
        private TestDiscoveryOptionsDialogPage _testDiscoveryOptions;
        private TestExecutionOptionsDialogPage _testExecutionOptions;
        private GoogleTestOptionsDialogPage _googleTestOptions;

        private DebuggerAttacherServiceHost _debuggerAttacherServiceHost;
        private GoogleTestAdapterAutomation _automation;
        private VisualStudioConfigurationTracker _visualStudioConfigurationTracker;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            var componentModel = (IComponentModel) await GetServiceAsync(typeof(SComponentModel));
            _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

            VsSettingsStorage.Init(this);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            DoInitialize();
        }

        private void DoInitialize()
        {
            _visualStudioConfigurationTracker = new VisualStudioConfigurationTracker(this,
                new ActivityLogLogger(this, () => OutputMode.Verbose).LogError);
            InitializeOptions();
            InitializeCommands();
            InitializeDebuggerAttacherService();
            DisplayReleaseNotesIfNecessary();
            WarnIfTestAdapterForGoogleTestIsInstalled();
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

        protected override object GetAutomationObject(string name)
        {
            if (name != GoogleTestAdapterAutomation.Name)
                return base.GetAutomationObject(name);

            return _automation ?? (_automation = new GoogleTestAdapterAutomation(
                () => _globalRunSettings?.RunSettings == null ? null : new RunSettingsService(_globalRunSettings),
                Common.Strings.Instance.ExtensionName,
                typeof(GoogleTestExtensionOptionsPage).Assembly.GetName().Version));
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
                _visualStudioConfigurationTracker?.Dispose();

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
            var logger = new ActivityLogLogger(this, () => OutputMode.Verbose);
            var visualStudioConfiguration = VisualStudioConfiguration.FromServiceProvider(this, logger.LogError);

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
                TearDownTestSuiteTestCases = _testDiscoveryOptions.TearDownTestSuiteTestCases,
                AdditionalTestDiscoveryParam = _testDiscoveryOptions.AdditionalTestDiscoveryParams,

                AdditionalPdbs = _testExecutionOptions.AdditionalPdbs,
                WorkingDir = _testExecutionOptions.WorkingDir,
                PathExtension = _testExecutionOptions.PathExtension,
                EnvironmentVariables = _testExecutionOptions.EnvironmentVariables,
                UseCTestTestProperties = _testExecutionOptions.UseCTestTestProperties,
                TestTimeoutInSeconds = _testExecutionOptions.TestTimeoutInSeconds,
                SourcePathMapping = _testExecutionOptions.SourcePathMapping,
                MapOnlyMissingSourcePaths = _testExecutionOptions.MapOnlyMissingSourcePaths,
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
                SolutionDir = visualStudioConfiguration.SolutionDir,
                PlatformName = visualStudioConfiguration.PlatformName,
                ConfigurationName = visualStudioConfiguration.ConfigurationName
            };
        }

    }

}