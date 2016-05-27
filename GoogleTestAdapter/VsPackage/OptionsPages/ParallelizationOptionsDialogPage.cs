using System.ComponentModel;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class ParallelizationOptionsDialogPage : NotifyingDialogPage
    {
        [Category(SettingsWrapper.CategoryParallelizationName)]
        [DisplayName(SettingsWrapper.OptionEnableParallelTestExecution)]
        [Description(SettingsWrapper.OptionEnableParallelTestExecutionDescription)]
        public bool EnableParallelTestExecution
        {
            get { return _enableParallelTestExecution; }
            set { SetAndNotify(ref _enableParallelTestExecution, value); }
        }
        private bool _enableParallelTestExecution = SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue;

        [Category(SettingsWrapper.CategoryParallelizationName)]
        [DisplayName(SettingsWrapper.OptionMaxNrOfThreads)]
        [Description(SettingsWrapper.OptionMaxNrOfThreadsDescription)]
        public int MaxNrOfThreads
        {
            get { return _maxNrOfThreads; }
            set { SetAndNotify(ref _maxNrOfThreads, value); }
        }
        private int _maxNrOfThreads = SettingsWrapper.OptionMaxNrOfThreadsDefaultValue;
    }

}