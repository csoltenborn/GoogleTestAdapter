using System.ComponentModel;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class GoogleTestOptionsDialogPage : NotifyingDialogPage
    {
        #region Runtime behavior

        [Category(Options.CategoryRuntimeBehaviorName)]
        [DisplayName(Options.OptionCatchExceptions)]
        [Description(Options.OptionCatchExceptionsDescription)]
        public bool CatchExceptions
        {
            get { return catchExceptions; }
            set { SetAndNotify(ref catchExceptions, value); }
        }
        private bool catchExceptions = Options.OptionCatchExceptionsDefaultValue;

        [Category(Options.CategoryRuntimeBehaviorName)]
        [DisplayName(Options.OptionBreakOnFailure)]
        [Description(Options.OptionBreakOnFailureDescription)]
        public bool BreakOnFailure
        {
            get { return breakOnFailure; }
            set { SetAndNotify(ref breakOnFailure, value); }
        }
        private bool breakOnFailure = Options.OptionBreakOnFailureDefaultValue;

        #endregion

        #region Test execution

        [Category(Options.CategoryTestExecutionName)]
        [DisplayName(Options.OptionRunDisabledTests)]
        [Description(Options.OptionRunDisabledTestsDescription)]
        public bool RunDisabledTests
        {
            get { return runDisabledTests; }
            set { SetAndNotify(ref runDisabledTests, value); }
        }
        private bool runDisabledTests = Options.OptionRunDisabledTestsDefaultValue;

        [Category(Options.CategoryTestExecutionName)]
        [DisplayName(Options.OptionNrOfTestRepetitions)]
        [Description(Options.OptionNrOfTestRepetitionsDescription)]
        public int NrOfTestRepetitions
        {
            get { return nrOfTestRepetitions; }
            set { SetAndNotify(ref nrOfTestRepetitions, value); }
        }
        private int nrOfTestRepetitions = Options.OptionNrOfTestRepetitionsDefaultValue;

        [Category(Options.CategoryTestExecutionName)]
        [DisplayName(Options.OptionShuffleTests)]
        [Description(Options.OptionShuffleTestsDescription)]
        public bool ShuffleTests
        {
            get { return shuffleTests; }
            set { SetAndNotify(ref shuffleTests, value); }
        }
        private bool shuffleTests = Options.OptionShuffleTestsDefaultValue;

        [Category(Options.CategoryTestExecutionName)]
        [DisplayName(Options.OptionShuffleTestsSeed)]
        [Description(Options.OptionShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed
        {
            get { return shuffleTestsSeed; }
            set { SetAndNotify(ref shuffleTestsSeed, value); }
        }
        private int shuffleTestsSeed = Options.OptionShuffleTestsSeedDefaultValue;

        #endregion
    }

}