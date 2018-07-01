// This file has been modified by Microsoft on 7/2017.

using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.VsPackage.Commands;
using GoogleTestAdapter.VsPackage.Debugging;
using GoogleTestAdapter.VsPackage.Helpers;
using GoogleTestAdapter.VsPackage.OptionsPages;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.IO;
using System.ServiceModel;
using EnvDTE;

namespace GoogleTestAdapter.VsPackage
{

    public partial class GoogleTestExtensionOptionsPage : IGoogleTestExtensionOptionsPage, IDisposable
    {
        private const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";

        private readonly string _debuggingNamedPipeId = Guid.NewGuid().ToString();

        private IGlobalRunSettingsInternal _globalRunSettings;

        private GeneralOptionsDialogPage _generalOptions;
        private ParallelizationOptionsDialogPage _parallelizationOptions;
        private GoogleTestOptionsDialogPage _googleTestOptions;

        // ReSharper disable once NotAccessedField.Local
        private DebuggerAttacherServiceHost _debuggerAttacherServiceHost;

        protected override void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

            _generalOptions = (GeneralOptionsDialogPage)GetDialogPage(typeof(GeneralOptionsDialogPage));
            _parallelizationOptions = (ParallelizationOptionsDialogPage)GetDialogPage(typeof(ParallelizationOptionsDialogPage));
            _googleTestOptions = (GoogleTestOptionsDialogPage)GetDialogPage(typeof(GoogleTestOptionsDialogPage));

            _globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();

            _generalOptions.PropertyChanged += OptionsChanged;
            _parallelizationOptions.PropertyChanged += OptionsChanged;
            _googleTestOptions.PropertyChanged += OptionsChanged;

            SwitchCatchExceptionsOptionCommand.Initialize(this);
            SwitchBreakOnFailureOptionCommand.Initialize(this);
            SwitchParallelExecutionOptionCommand.Initialize(this);
            SwitchPrintTestOutputOptionCommand.Initialize(this);

            DisplayReleaseNotesIfNecessary();

            var logger = new ActivityLogLogger(this, () => _generalOptions.DebugMode);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _generalOptions?.Dispose();
                _parallelizationOptions?.Dispose();
                _googleTestOptions?.Dispose();

                try
                {
                    // Cannot simply do ".?" because Code Analysis does not understand that.
                    if (_debuggerAttacherServiceHost != null)
                    {
                        _debuggerAttacherServiceHost.Close();
                    }
                }
                catch (CommunicationException)
                {
                    _debuggerAttacherServiceHost.Abort();
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
            get { return _parallelizationOptions.EnableParallelTestExecution; }
            set
            {
                _parallelizationOptions.EnableParallelTestExecution = value;
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
                TestDiscoveryRegex = _generalOptions.TestDiscoveryRegex,
                AdditionalPdbs = _generalOptions.AdditionalPdbs,
                TestDiscoveryTimeoutInSeconds = _generalOptions.TestDiscoveryTimeoutInSeconds,
                WorkingDir = _generalOptions.WorkingDir,
                PathExtension = _generalOptions.PathExtension,
                TraitsRegexesBefore = _generalOptions.TraitsRegexesBefore,
                TraitsRegexesAfter = _generalOptions.TraitsRegexesAfter,
                TestNameSeparator = _generalOptions.TestNameSeparator,
                ParseSymbolInformation = _generalOptions.ParseSymbolInformation,
                DebugMode = _generalOptions.DebugMode,
                TimestampOutput = _generalOptions.TimestampOutput,
                ShowReleaseNotes = ShowReleaseNotes,
                AdditionalTestExecutionParam = _generalOptions.AdditionalTestExecutionParams,
                BatchForTestSetup = _generalOptions.BatchForTestSetup,
                BatchForTestTeardown = _generalOptions.BatchForTestTeardown,
                KillProcessesOnCancel = _generalOptions.KillProcessesOnCancel,
                SkipOriginCheck = _generalOptions.SkipOriginCheck,

                CatchExceptions = _googleTestOptions.CatchExceptions,
                BreakOnFailure = _googleTestOptions.BreakOnFailure,
                RunDisabledTests = _googleTestOptions.RunDisabledTests,
                NrOfTestRepetitions = _googleTestOptions.NrOfTestRepetitions,
                ShuffleTests = _googleTestOptions.ShuffleTests,
                ShuffleTestsSeed = _googleTestOptions.ShuffleTestsSeed,

                ParallelTestExecution = _parallelizationOptions.EnableParallelTestExecution,
                MaxNrOfThreads = _parallelizationOptions.MaxNrOfThreads,

                UseNewTestExecutionFramework = _generalOptions.UseNewTestExecutionFramework2,

                DebuggingNamedPipeId = _debuggingNamedPipeId,
                SolutionDir = solutionDir,
                PlatformName = platformName,
                ConfigurationName = configurationName
            };
        }

        private void GetVisualStudioConfiguration(out string solutionDir, out string platformName, out string configurationName)
        {
            solutionDir = platformName = configurationName = null;

            try
            {
                if (GetService(typeof(DTE)) is DTE dte)
                {
                    solutionDir = Path.GetDirectoryName(dte.Solution.FullName);

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
                new ActivityLogLogger(this, () => true)
                    .LogError($"Exception while receiving configuration info from Visual Studio{Environment.NewLine}{e}");
            }
        }
    }

}
