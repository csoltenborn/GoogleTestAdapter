using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        #region Test execution

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionTestDiscoveryRegex)]
        [Description(SettingsWrapper.OptionTestDiscoveryRegexDescription)]
        public string TestDiscoveryRegex
        {
            get { return _testDiscoveryRegex; }
            set { SetAndNotify(ref _testDiscoveryRegex, value); }
        }
        private string _testDiscoveryRegex = SettingsWrapper.OptionTestDiscoveryRegexDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionWorkingDir)]
        [Description(SettingsWrapper.OptionWorkingDirDescription)]
        public string WorkingDir
        {
            get { return _workingDirectory; }
            set { SetAndNotify(ref _workingDirectory, value); }
        }
        private string _workingDirectory = SettingsWrapper.OptionWorkingDirDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionPathExtension)]
        [Description(SettingsWrapper.OptionPathExtensionDescription)]
        public string PathExtension
        {
            get { return _pathExtension; }
            set { SetAndNotify(ref _pathExtension, value); }
        }
        private string _pathExtension = SettingsWrapper.OptionPathExtensionDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionAdditionalTestExecutionParams)]
        [Description(SettingsWrapper.OptionAdditionalTestExecutionParamsDescription)]
        public string AdditionalTestExecutionParams
        {
            get { return _additionalTestExecutionParams; }
            set { SetAndNotify(ref _additionalTestExecutionParams, value); }
        }
        private string _additionalTestExecutionParams = SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionBatchForTestSetup)]
        [Description(SettingsWrapper.OptionBatchForTestSetupDescription)]
        public string BatchForTestSetup
        {
            get { return _batchForTestSetup; }
            set { SetAndNotify(ref _batchForTestSetup, value); }
        }
        private string _batchForTestSetup = SettingsWrapper.OptionBatchForTestSetupDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionBatchForTestTeardown)]
        [Description(SettingsWrapper.OptionBatchForTestTeardownDescription)]
        public string BatchForTestTeardown
        {
            get { return _batchForTestTeardown; }
            set { SetAndNotify(ref _batchForTestTeardown, value); }
        }
        private string _batchForTestTeardown = SettingsWrapper.OptionBatchForTestTeardownDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionKillProcessesOnCancel)]
        [Description(SettingsWrapper.OptionKillProcessesOnCancelDescription)]
        public bool KillProcessesOnCancel
        {
            get { return _killProcessesOnCancel; }
            set { SetAndNotify(ref _killProcessesOnCancel, value); }
        }
        private bool _killProcessesOnCancel = SettingsWrapper.OptionKillProcessesOnCancelDefaultValue;

        #endregion

        #region Misc

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionUseNewTestExecutionFramework)]
        [Description(SettingsWrapper.OptionUseNewTestExecutionFrameworkDescription)]
        public bool UseNewTestExecutionFramework2
        {
            get { return _useNewTestExecutionFramework; }
            set { SetAndNotify(ref _useNewTestExecutionFramework, value); }
        }
        private bool _useNewTestExecutionFramework = SettingsWrapper.OptionUseNewTestExecutionFrameworkDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionPrintTestOutput)]
        [Description(SettingsWrapper.OptionPrintTestOutputDescription)]
        public bool PrintTestOutput
        {
            get { return _printTestOutput; }
            set { SetAndNotify(ref _printTestOutput, value); }
        }
        private bool _printTestOutput = SettingsWrapper.OptionPrintTestOutputDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionTestNameSeparator)]
        [Description(SettingsWrapper.OptionTestNameSeparatorDescription)]
        public string TestNameSeparator
        {
            get { return _testNameSeparator; }
            set { SetAndNotify(ref _testNameSeparator, value); }
        }
        private string _testNameSeparator = SettingsWrapper.OptionTestNameSeparatorDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionParseSymbolInformation)]
        [Description(SettingsWrapper.OptionParseSymbolInformationDescription)]
        public bool ParseSymbolInformation
        {
            get { return _parseSymbolInformation; }
            set { SetAndNotify(ref _parseSymbolInformation, value); }
        }
        private bool _parseSymbolInformation = SettingsWrapper.OptionParseSymbolInformationDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionDebugMode)]
        [Description(SettingsWrapper.OptionDebugModeDescription)]
        public bool DebugMode
        {
            get { return _debugMode; }
            set { SetAndNotify(ref _debugMode, value); }
        }
        private bool _debugMode = SettingsWrapper.OptionDebugModeDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionTimestampOutput)]
        [Description(SettingsWrapper.OptionTimestampOutputDescription)]
        public bool TimestampOutput
        {
            get { return _timestampOutput; }
            set { SetAndNotify(ref _timestampOutput, value); }
        }
        private bool _timestampOutput = SettingsWrapper.OptionTimestampOutputDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionShowReleaseNotes)]
        [Description(SettingsWrapper.OptionShowReleaseNotesDescription)]
        public bool ShowReleaseNotes
        {
            get { return _showReleaseNotes; }
            set { SetAndNotify(ref _showReleaseNotes, value); }
        }
        private bool _showReleaseNotes = SettingsWrapper.OptionShowReleaseNotesDefaultValue;

        #endregion
    }

}