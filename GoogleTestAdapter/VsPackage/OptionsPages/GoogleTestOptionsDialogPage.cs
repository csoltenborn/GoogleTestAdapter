using System.ComponentModel;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class GoogleTestOptionsDialogPage : NotifyingDialogPage
    {
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionCatchExceptions)]
        [Description(Options.OptionCatchExceptionsDescription)]
        public bool CatchExceptions
        {
            get { return catchExceptions; }
            set { SetAndNotify(ref catchExceptions, value); }
        }
        private bool catchExceptions = Options.OptionCatchExceptionsDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionBreakOnFailure)]
        [Description(Options.OptionBreakOnFailureDescription)]
        public bool BreakOnFailure
        {
            get { return breakOnFailure; }
            set { SetAndNotify(ref breakOnFailure, value); }
        }
        private bool breakOnFailure = Options.OptionBreakOnFailureDefaultValue;

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

    }

}