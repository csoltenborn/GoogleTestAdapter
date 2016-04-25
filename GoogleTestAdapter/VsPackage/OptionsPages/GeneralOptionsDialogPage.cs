using System.ComponentModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        #region Test execution

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionTestDiscoveryRegex)]
        [Description(SettingsWrapper.OptionTestDiscoveryRegexDescription)]
        public string TestDiscoveryRegex
        {
            get { return testDiscoveryRegex; }
            set { SetAndNotify(ref testDiscoveryRegex, value); }
        }
        private string testDiscoveryRegex = SettingsWrapper.OptionTestDiscoveryRegexDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionPathExtension)]
        [Description(SettingsWrapper.OptionPathExtensionDescription)]
        public string PathExtension
        {
            get { return pathExtension; }
            set { SetAndNotify(ref pathExtension, value); }
        }
        private string pathExtension = SettingsWrapper.OptionPathExtensionDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionAdditionalTestExecutionParams)]
        [Description(SettingsWrapper.OptionAdditionalTestExecutionParamsDescription)]
        public string AdditionalTestExecutionParams
        {
            get { return additionalTestExecutionParams; }
            set { SetAndNotify(ref additionalTestExecutionParams, value); }
        }
        private string additionalTestExecutionParams = SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionBatchForTestSetup)]
        [Description(SettingsWrapper.OptionBatchForTestSetupDescription)]
        public string BatchForTestSetup
        {
            get { return batchForTestSetup; }
            set { SetAndNotify(ref batchForTestSetup, value); }
        }
        private string batchForTestSetup = SettingsWrapper.OptionBatchForTestSetupDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionBatchForTestTeardown)]
        [Description(SettingsWrapper.OptionBatchForTestTeardownDescription)]
        public string BatchForTestTeardown
        {
            get { return batchForTestTeardown; }
            set { SetAndNotify(ref batchForTestTeardown, value); }
        }
        private string batchForTestTeardown = SettingsWrapper.OptionBatchForTestTeardownDefaultValue;

        #endregion

        #region Traits

        [Category(SettingsWrapper.CategoryTraitsName)]
        [DisplayName(SettingsWrapper.OptionTraitsRegexesBefore)]
        [Description(SettingsWrapper.OptionTraitsDescription)]
        public string TraitsRegexesBefore
        {
            get { return traitsRegexesBefore; }
            set { SetAndNotify(ref traitsRegexesBefore, value); }
        }
        private string traitsRegexesBefore = SettingsWrapper.OptionTraitsRegexesDefaultValue;

        [Category(SettingsWrapper.CategoryTraitsName)]
        [DisplayName(SettingsWrapper.OptionTraitsRegexesAfter)]
        [Description(SettingsWrapper.OptionTraitsDescription)]
        public string TraitsRegexesAfter
        {
            get { return traitsRegexesAfter; }
            set { SetAndNotify(ref traitsRegexesAfter, value); }
        }
        private string traitsRegexesAfter = SettingsWrapper.OptionTraitsRegexesDefaultValue;

        #endregion

        #region Misc

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionPrintTestOutput)]
        [Description(SettingsWrapper.OptionPrintTestOutputDescription)]
        public bool PrintTestOutput
        {
            get { return printTestOutput; }
            set { SetAndNotify(ref printTestOutput, value); }
        }
        private bool printTestOutput = SettingsWrapper.OptionPrintTestOutputDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionTestNameSeparator)]
        [Description(SettingsWrapper.OptionTestNameSeparatorDescription)]
        public string TestNameSeparator
        {
            get { return testNameSeparator; }
            set { SetAndNotify(ref testNameSeparator, value); }
        }
        private string testNameSeparator = SettingsWrapper.OptionTestNameSeparatorDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionParseSymbolInformation)]
        [Description(SettingsWrapper.OptionParseSymbolInformationDescription)]
        public bool ParseSymbolInformation
        {
            get { return parseSymbolInformation; }
            set { SetAndNotify(ref parseSymbolInformation, value); }
        }
        private bool parseSymbolInformation = SettingsWrapper.OptionParseSymbolInformationDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionDebugMode)]
        [Description(SettingsWrapper.OptionDebugModeDescription)]
        public bool DebugMode
        {
            get { return debugMode; }
            set { SetAndNotify(ref debugMode, value); }
        }
        private bool debugMode = SettingsWrapper.OptionDebugModeDefaultValue;

        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionShowReleaseNotes)]
        [Description(SettingsWrapper.OptionShowReleaseNotesDescription)]
        public bool ShowReleaseNotes
        {
            get { return showReleaseNotes; }
            set { SetAndNotify(ref showReleaseNotes, value); }
        }
        private bool showReleaseNotes = SettingsWrapper.OptionShowReleaseNotesDefaultValue;

        #endregion
    }

}