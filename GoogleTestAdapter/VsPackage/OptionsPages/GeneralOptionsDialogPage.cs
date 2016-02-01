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
        [DisplayName(Options.OptionRunDisabledTests)]
        [Description(Options.OptionRunDisabledTestsDescription)]
        public bool RunDisabledTests
        {
            get { return runDisabledTests; }
            set { SetAndNotify(ref runDisabledTests, value); }
        }
        private bool runDisabledTests = Options.OptionRunDisabledTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionNrOfTestRepetitions)]
        [Description(Options.OptionNrOfTestRepetitionsDescription)]
        public int NrOfTestRepetitions
        {
            get { return nrOfTestRepetitions; }
            set { SetAndNotify(ref nrOfTestRepetitions, value); }
        }
        private int nrOfTestRepetitions = Options.OptionNrOfTestRepetitionsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTests)]
        [Description(Options.OptionShuffleTestsDescription)]
        public bool ShuffleTests
        {
            get { return shuffleTests; }
            set { SetAndNotify(ref shuffleTests, value); }
        }
        private bool shuffleTests = Options.OptionShuffleTestsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionShuffleTestsSeed)]
        [Description(Options.OptionShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed
        {
            get { return shuffleTestsSeed; }
            set { SetAndNotify(ref shuffleTestsSeed, value); }
        }
        private int shuffleTestsSeed = Options.OptionShuffleTestsSeedDefaultValue;

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
        [DisplayName(Options.OptionAdditionalTestExecutionParams)]
        [Description(Options.OptionAdditionalTestExecutionParamsDescription)]
        public string AdditionalTestExecutionParams
        {
            get { return additionalTestExecutionParams; }
            set { SetAndNotify(ref additionalTestExecutionParams, value); }
        }
        private string additionalTestExecutionParams = Options.OptionAdditionalTestExecutionParamsDefaultValue;
    }

}