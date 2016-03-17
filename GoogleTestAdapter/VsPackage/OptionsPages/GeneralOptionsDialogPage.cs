using System.ComponentModel;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionPrintTestOutput)]
        [Description(Options.OptionPrintTestOutputDescription)]
        public bool PrintTestOutput
        {
            get { return printTestOutput; }
            set { SetAndNotify(ref printTestOutput, value); }
        }
        private bool printTestOutput = Options.OptionPrintTestOutputDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestDiscoveryRegex)]
        [Description(Options.OptionTestDiscoveryRegexDescription)]
        public string TestDiscoveryRegex
        {
            get { return testDiscoveryRegex; }
            set { SetAndNotify(ref testDiscoveryRegex, value); }
        }
        private string testDiscoveryRegex = Options.OptionTestDiscoveryRegexDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionPathExtension)]
        [Description(Options.OptionPathExtensionDescription)]
        public string PathExtension
        {
            get { return pathExtension; }
            set { SetAndNotify(ref pathExtension, value); }
        }
        private string pathExtension = Options.OptionPathExtensionDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionDebugMode)]
        [Description(Options.OptionDebugModeDescription)]
        public bool DebugMode
        {
            get { return debugMode; }
            set { SetAndNotify(ref debugMode, value); }
        }
        private bool debugMode = Options.OptionDebugModeDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesBefore)]
        [Description(Options.OptionTraitsDescription)]
        public string TraitsRegexesBefore
        {
            get { return traitsRegexesBefore; }
            set { SetAndNotify(ref traitsRegexesBefore, value); }
        }
        private string traitsRegexesBefore = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTraitsRegexesAfter)]
        [Description(Options.OptionTraitsDescription)]
        public string TraitsRegexesAfter
        {
            get { return traitsRegexesAfter; }
            set { SetAndNotify(ref traitsRegexesAfter, value); }
        }
        private string traitsRegexesAfter = Options.OptionTraitsRegexesDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionTestNameSeparator)]
        [Description(Options.OptionTestNameSeparatorDescription)]
        public string TestNameSeparator
        {
            get { return testNameSeparator; }
            set { SetAndNotify(ref testNameSeparator, value); }
        }
        private string testNameSeparator = Options.OptionTestNameSeparatorDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionAdditionalTestExecutionParams)]
        [Description(Options.OptionAdditionalTestExecutionParamsDescription)]
        public string AdditionalTestExecutionParams
        {
            get { return additionalTestExecutionParams; }
            set { SetAndNotify(ref additionalTestExecutionParams, value); }
        }
        private string additionalTestExecutionParams = Options.OptionAdditionalTestExecutionParamsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionBatchForTestSetup)]
        [Description(Options.OptionBatchForTestSetupDescription)]
        public string BatchForTestSetup
        {
            get { return batchForTestSetup; }
            set { SetAndNotify(ref batchForTestSetup, value); }
        }
        private string batchForTestSetup = Options.OptionBatchForTestSetupDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionBatchForTestTeardown)]
        [Description(Options.OptionBatchForTestTeardownDescription)]
        public string BatchForTestTeardown
        {
            get { return batchForTestTeardown; }
            set { SetAndNotify(ref batchForTestTeardown, value); }
        }
        private string batchForTestTeardown = Options.OptionBatchForTestTeardownDefaultValue;
    }

}