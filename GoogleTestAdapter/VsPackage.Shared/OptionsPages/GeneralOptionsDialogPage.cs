// This file has been modified by Microsoft on 7/2017.

using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public partial class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        #region Test execution

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionTestDiscoveryRegex)]
        [Description(SettingsWrapper.OptionTestDiscoveryRegexDescription)]
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

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionAdditionalPdbs)]
        [Description(SettingsWrapper.OptionAdditionalPdbsDescription)]
        public string AdditionalPdbs
        {
            get { return _additionalPdbs; }
            set
            {
                Utils.ValidateRegex(value);
                SetAndNotify(ref _additionalPdbs, value);
            }
        }
        private string _additionalPdbs = SettingsWrapper.OptionAdditionalPdbsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionTestDiscoveryTimeoutInSeconds)]
        [Description(SettingsWrapper.OptionTestDiscoveryTimeoutInSecondsDescription)]
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

        #region Traits

        [Category(SettingsWrapper.CategoryTraitsName)]
        [DisplayName(SettingsWrapper.OptionTraitsRegexesBefore)]
        [Description(SettingsWrapper.OptionTraitsDescription)]
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

        [Category(SettingsWrapper.CategoryTraitsName)]
        [DisplayName(SettingsWrapper.OptionTraitsRegexesAfter)]
        [Description(SettingsWrapper.OptionTraitsDescription)]
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
            set
            {
                if (value.Length > 16)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Expected string not longer than 16 characters.");
                SetAndNotify(ref _testNameSeparator, value);
            }
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
        [DisplayName(SettingsWrapper.OptionSkipOriginCheck)]
        [Description(SettingsWrapper.OptionSkipOriginCheckDescription)]
        public bool SkipOriginCheck
        {
            get { return _skipOriginCheck; }
            set { SetAndNotify(ref _skipOriginCheck, value); }
        }
        private bool _skipOriginCheck = SettingsWrapper.OptionSkipOriginCheckDefaultValue;

        #endregion
    }

}