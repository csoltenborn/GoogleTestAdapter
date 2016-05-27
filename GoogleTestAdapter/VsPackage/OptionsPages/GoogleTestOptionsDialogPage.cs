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
            get { return _catchExceptions; }
            set { SetAndNotify(ref _catchExceptions, value); }
        }
        private bool _catchExceptions = SettingsWrapper.OptionCatchExceptionsDefaultValue;

        [Category(SettingsWrapper.CategoryRuntimeBehaviorName)]
        [DisplayName(SettingsWrapper.OptionBreakOnFailure)]
        [Description(SettingsWrapper.OptionBreakOnFailureDescription)]
        public bool BreakOnFailure
        {
            get { return _breakOnFailure; }
            set { SetAndNotify(ref _breakOnFailure, value); }
        }
        private bool _breakOnFailure = SettingsWrapper.OptionBreakOnFailureDefaultValue;

        #endregion

        #region Test execution

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionRunDisabledTests)]
        [Description(SettingsWrapper.OptionRunDisabledTestsDescription)]
        public bool RunDisabledTests
        {
            get { return _runDisabledTests; }
            set { SetAndNotify(ref _runDisabledTests, value); }
        }
        private bool _runDisabledTests = SettingsWrapper.OptionRunDisabledTestsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionNrOfTestRepetitions)]
        [Description(SettingsWrapper.OptionNrOfTestRepetitionsDescription)]
        public int NrOfTestRepetitions
        {
            get { return _nrOfTestRepetitions; }
            set { SetAndNotify(ref _nrOfTestRepetitions, value); }
        }
        private int _nrOfTestRepetitions = SettingsWrapper.OptionNrOfTestRepetitionsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionShuffleTests)]
        [Description(SettingsWrapper.OptionShuffleTestsDescription)]
        public bool ShuffleTests
        {
            get { return _shuffleTests; }
            set { SetAndNotify(ref _shuffleTests, value); }
        }
        private bool _shuffleTests = SettingsWrapper.OptionShuffleTestsDefaultValue;

        [Category(SettingsWrapper.CategoryTestExecutionName)]
        [DisplayName(SettingsWrapper.OptionShuffleTestsSeed)]
        [Description(SettingsWrapper.OptionShuffleTestsSeedDescription)]
        public int ShuffleTestsSeed
        {
            get { return _shuffleTestsSeed; }
            set { SetAndNotify(ref _shuffleTestsSeed, value); }
        }
        private int _shuffleTestsSeed = SettingsWrapper.OptionShuffleTestsSeedDefaultValue;

        #endregion
    }

}