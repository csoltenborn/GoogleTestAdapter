// This file has been modified by Microsoft on 7/2017.

using GoogleTestAdapter.Settings;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        #region Output

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionPrintTestOutput)]
        [Description(SettingsWrapper.OptionPrintTestOutputDescription)]
        public bool PrintTestOutput
        {
            get => _printTestOutput;
            set => SetAndNotify(ref _printTestOutput, value);
        }
        private bool _printTestOutput = SettingsWrapper.OptionPrintTestOutputDefaultValue;

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionDebugMode)]
        [Description(SettingsWrapper.OptionDebugModeDescription)]
        public OutputMode DebugMode
        {
            get => _debugMode;
            set => SetAndNotify(ref _debugMode, value);
        }
        private OutputMode _debugMode = SettingsWrapper.OptionDebugModeDefaultValue;

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionTimestampOutput)]
        [Description(SettingsWrapper.OptionTimestampOutputDescription)]
        public bool TimestampOutput
        {
            get => _timestampOutput;
            set => SetAndNotify(ref _timestampOutput, value);
        }
        private bool _timestampOutput = SettingsWrapper.OptionTimestampOutputDefaultValue;

        #endregion

        #region Security

        [Category(SettingsWrapper.CategorySecurityName)]
        [DisplayName(SettingsWrapper.OptionSkipOriginCheck)]
        [Description(SettingsWrapper.OptionSkipOriginCheckDescription)]
        public bool SkipOriginCheck
        {
            get => _skipOriginCheck;
            set => SetAndNotify(ref _skipOriginCheck, value);
        }
        private bool _skipOriginCheck = SettingsWrapper.OptionSkipOriginCheckDefaultValue;
        
        #endregion
    }

}