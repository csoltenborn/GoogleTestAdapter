// This file has been modified by Microsoft on 9/2017.

using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public partial class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        #region Test execution

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionTestDiscoveryRegex")]
        [LocalizedDescription("OptionTestDiscoveryRegexDescription")]
        public string TestDiscoveryRegex
        {
            get { return _testDiscoveryRegex; }
            set
            {
                Utils.ValidateRegex(value);
                SetAndNotify(ref _testDiscoveryRegex, value);
            }
        }
        private string _testDiscoveryRegex = SettingsWrapper.OptionTestDiscoveryRegexDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionTestDiscoveryTimeoutInSeconds")]
        [LocalizedDescription("OptionTestDiscoveryTimeoutInSecondsDescription")]
        public int TestDiscoveryTimeoutInSeconds
        {
            get { return _testDiscoveryTimeoutInSeconds; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Expected a number greater than or equal to 0.");
                SetAndNotify(ref _testDiscoveryTimeoutInSeconds, value);
            }
        }
        private int _testDiscoveryTimeoutInSeconds = SettingsWrapper.OptionTestDiscoveryTimeoutInSecondsDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionWorkingDir")]
        [LocalizedDescription("OptionWorkingDirDescription")]
        public string WorkingDir
        {
            get { return _workingDirectory; }
            set { SetAndNotify(ref _workingDirectory, value); }
        }
        private string _workingDirectory = SettingsWrapper.OptionWorkingDirDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionPathExtension")]
        [LocalizedDescription("OptionPathExtensionDescription")]
        public string PathExtension
        {
            get { return _pathExtension; }
            set { SetAndNotify(ref _pathExtension, value); }
        }
        private string _pathExtension = SettingsWrapper.OptionPathExtensionDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionAdditionalTestExecutionParams")]
        [LocalizedDescription("OptionAdditionalTestExecutionParamsDescription")]
        public string AdditionalTestExecutionParams
        {
            get { return _additionalTestExecutionParams; }
            set { SetAndNotify(ref _additionalTestExecutionParams, value); }
        }
        private string _additionalTestExecutionParams = SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionBatchForTestSetup")]
        [LocalizedDescription("OptionBatchForTestSetupDescription")]
        public string BatchForTestSetup
        {
            get { return _batchForTestSetup; }
            set { SetAndNotify(ref _batchForTestSetup, value); }
        }
        private string _batchForTestSetup = SettingsWrapper.OptionBatchForTestSetupDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionBatchForTestTeardown")]
        [LocalizedDescription("OptionBatchForTestTeardownDescription")]
        public string BatchForTestTeardown
        {
            get { return _batchForTestTeardown; }
            set { SetAndNotify(ref _batchForTestTeardown, value); }
        }
        private string _batchForTestTeardown = SettingsWrapper.OptionBatchForTestTeardownDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionKillProcessesOnCancel")]
        [LocalizedDescription("OptionKillProcessesOnCancelDescription")]
        public bool KillProcessesOnCancel
        {
            get { return _killProcessesOnCancel; }
            set { SetAndNotify(ref _killProcessesOnCancel, value); }
        }
        private bool _killProcessesOnCancel = SettingsWrapper.OptionKillProcessesOnCancelDefaultValue;

        #endregion

        #region Traits

        [LocalizedCategory("CategoryTraitsName")]
        [LocalizedDisplayName("OptionTraitsRegexesBefore")]
        [LocalizedDescription("OptionTraitsDescription")]
        public string TraitsRegexesBefore
        {
            get { return _traitsRegexesBefore; }
            set
            {
                Utils.ValidateTraitRegexes(value);
                SetAndNotify(ref _traitsRegexesBefore, value);
            }
        }
        private string _traitsRegexesBefore = SettingsWrapper.OptionTraitsRegexesDefaultValue;

        [LocalizedCategory("CategoryTraitsName")]
        [LocalizedDisplayName("OptionTraitsRegexesAfter")]
        [LocalizedDescription("OptionTraitsDescription")]
        public string TraitsRegexesAfter
        {
            get { return _traitsRegexesAfter; }
            set
            {
                Utils.ValidateTraitRegexes(value);
                SetAndNotify(ref _traitsRegexesAfter, value);
            }
        }
        private string _traitsRegexesAfter = SettingsWrapper.OptionTraitsRegexesDefaultValue;

        #endregion

        #region Misc

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionUseNewTestExecutionFramework")]
        [LocalizedDescription("OptionUseNewTestExecutionFrameworkDescription")]
        public bool UseNewTestExecutionFramework2
        {
            get { return _useNewTestExecutionFramework; }
            set { SetAndNotify(ref _useNewTestExecutionFramework, value); }
        }
        private bool _useNewTestExecutionFramework = SettingsWrapper.OptionUseNewTestExecutionFrameworkDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionPrintTestOutput")]
        [LocalizedDescription("OptionPrintTestOutputDescription")]
        public bool PrintTestOutput
        {
            get { return _printTestOutput; }
            set { SetAndNotify(ref _printTestOutput, value); }
        }
        private bool _printTestOutput = SettingsWrapper.OptionPrintTestOutputDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionTestNameSeparator")]
        [LocalizedDescription("OptionTestNameSeparatorDescription")]
        public string TestNameSeparator
        {
            get { return _testNameSeparator; }
            set
            {
                if (value.Length > 16)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Expected string not longer than 16 characters.");
                SetAndNotify(ref _testNameSeparator, value);
            }
        }
        private string _testNameSeparator = SettingsWrapper.OptionTestNameSeparatorDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionParseSymbolInformation")]
        [LocalizedDescription("OptionParseSymbolInformationDescription")]
        public bool ParseSymbolInformation
        {
            get { return _parseSymbolInformation; }
            set { SetAndNotify(ref _parseSymbolInformation, value); }
        }
        private bool _parseSymbolInformation = SettingsWrapper.OptionParseSymbolInformationDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionDebugMode")]
        [LocalizedDescription("OptionDebugModeDescription")]
        public bool DebugMode
        {
            get { return _debugMode; }
            set { SetAndNotify(ref _debugMode, value); }
        }
        private bool _debugMode = SettingsWrapper.OptionDebugModeDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionTimestampOutput")]
        [LocalizedDescription("OptionTimestampOutputDescription")]
        public bool TimestampOutput
        {
            get { return _timestampOutput; }
            set { SetAndNotify(ref _timestampOutput, value); }
        }
        private bool _timestampOutput = SettingsWrapper.OptionTimestampOutputDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionAdditionalTestDiscoveryParams")]
        [LocalizedDescription("OptionAdditionalTestDiscoveryParamsDescription")]
        public string AdditionalTestDiscoveryParams
        {
            get { return _additionalTestDiscoveryParams; }
            set { SetAndNotify(ref _additionalTestDiscoveryParams, value); }
        }
        private string _additionalTestDiscoveryParams = SettingsWrapper.OptionAdditionalTestDiscoveryParamsDefaultValue;

        #endregion
    }

}