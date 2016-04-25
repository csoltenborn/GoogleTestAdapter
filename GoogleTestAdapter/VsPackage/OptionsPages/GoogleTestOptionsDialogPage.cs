using System.ComponentModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class GoogleTestOptionsDialogPage : NotifyingDialogPage
    {
        #region Runtime behavior

        [Category(SettingsWrapper.CategoryRuntimeBehaviorName)]
        [DisplayName(SettingsWrapper.OptionCatchExceptions)]
        [Description(SettingsWrapper.OptionCatchExceptionsDescription)]
        public bool CatchExceptions
        {
            get { return catchExceptions; }
            set { SetAndNotify(ref catchExceptions, value); }
        }
        private bool catchExceptions = SettingsWrapper.OptionCatchExceptionsDefaultValue;

        [Category(SettingsWrapper.CategoryRuntimeBehaviorName)]
        [DisplayName(SettingsWrapper.OptionBreakOnFailure)]
        [Description(SettingsWrapper.OptionBreakOnFailureDescription)]
        public bool BreakOnFailure
        {
            get { return breakOnFailure; }
            set { SetAndNotify(ref breakOnFailure, value); }
        }
        private bool breakOnFailure = SettingsWrapper.OptionBreakOnFailureDefaultValue;

        #endregion

        #region Test execution

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionRunDisabledTests)]
        [Description(SettingsWrapper.OptionRunDisabledTestsDescription)]
        public bool RunDisabledTests
        {
            get { return runDisabledTests; }
            set { SetAndNotify(ref runDisabledTests, value); }
        }
        private bool runDisabledTests = SettingsWrapper.OptionRunDisabledTestsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionNrOfTestRepetitions)]
        [Description(SettingsWrapper.OptionNrOfTestRepetitionsDescription)]
        public int NrOfTestRepetitions
        {
            get { return nrOfTestRepetitions; }
            set { SetAndNotify(ref nrOfTestRepetitions, value); }
        }
        private int nrOfTestRepetitions = SettingsWrapper.OptionNrOfTestRepetitionsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionShuffleTests)]
        [Description(SettingsWrapper.OptionShuffleTestsDescription)]
        public bool ShuffleTests
        {
            get { return shuffleTests; }
            set { SetAndNotify(ref shuffleTests, value); }
        }
        private bool shuffleTests = SettingsWrapper.OptionShuffleTestsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionShuffleTestsSeed)]
        [Description(SettingsWrapper.OptionShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed
        {
            get { return shuffleTestsSeed; }
            set { SetAndNotify(ref shuffleTestsSeed, value); }
        }
        private int shuffleTestsSeed = SettingsWrapper.OptionShuffleTestsSeedDefaultValue;

        #endregion
    }

}