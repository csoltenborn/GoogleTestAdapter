// This file has been modified by Microsoft on 9/2017.

using GoogleTestAdapter.Settings;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GoogleTestAdapter.Common;
using Microsoft.VisualStudio.Shell;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        #region Output

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionOutputMode)]
        [Description(SettingsWrapper.OptionOutputModeDescription)]
        public OutputMode OutputMode
        {
            get => _outputMode;
            set => SetAndNotify(ref _outputMode, value);
        }
        private OutputMode _outputMode = SettingsWrapper.OptionOutputModeDefaultValue;

        [Category(SettingsWrapper.CategoryOutputName)]
        [LocalizedDisplayName("OptionTimestampOutput")]
        [LocalizedDescription("OptionTimestampOutputDescription")]
        [PropertyPageTypeConverter(typeof(TimestampModeConverter))]
        public TimestampMode TimestampMode
        {
            get => _timestampMode;
            set => SetAndNotify(ref _timestampMode, value);
        }

        private TimestampMode _timestampMode = SettingsWrapper.OptionTimestampOutputDefaultValue;

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionSeverityMode)]
        [Description(SettingsWrapper.OptionSeverityModeDescription)]
        [PropertyPageTypeConverter(typeof(SeverityModeConverter))]
        public SeverityMode SeverityMode
        {
            get => _severityMode;
            set => SetAndNotify(ref _severityMode, value);
        }
        private SeverityMode _severityMode = SettingsWrapper.OptionSeverityModeDefaultValue;

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionSummaryMode)]
        [Description(SettingsWrapper.OptionSummaryModeDescription)]
        [PropertyPageTypeConverter(typeof(SummaryModeConverter))]
        public SummaryMode SummaryMode
        {
            get => _summaryMode;
            set => SetAndNotify(ref _summaryMode, value);
        }
        private SummaryMode _summaryMode = SettingsWrapper.OptionSummaryModeDefaultValue;

        [Category(SettingsWrapper.CategoryOutputName)]
        [DisplayName(SettingsWrapper.OptionPrefixOutputWithGta)]
        [Description(SettingsWrapper.OptionPrefixOutputWithGtaDescription)]
        public bool PrefixOutputWithGta
        {
            get => _prefixOutputWithGta;
            set => SetAndNotify(ref _prefixOutputWithGta, value);
        }
        private bool _prefixOutputWithGta = SettingsWrapper.OptionPrefixOutputWithGtaDefaultValue;

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
        [LocalizedDisplayName("OptionDebugMode")]
        [LocalizedDescription("OptionDebugModeDescription")]
        public bool DebugMode
        {
            get { return _debugMode; }
            set { SetAndNotify(ref _debugMode, value); }
        }
        private bool _debugMode = SettingsWrapper.OptionDebugModeDefaultValue;

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