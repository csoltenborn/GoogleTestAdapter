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
            get { return enableParallelTestExecution; }
            set { SetAndNotify(ref enableParallelTestExecution, value); }
        }
        private bool enableParallelTestExecution = SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue;

        [Category(SettingsWrapper.CategoryParallelizationName)]
        [DisplayName(SettingsWrapper.OptionMaxNrOfThreads)]
        [Description(SettingsWrapper.OptionMaxNrOfThreadsDescription)]
        public int MaxNrOfThreads
        {
            get { return maxNrOfThreads; }
            set { SetAndNotify(ref maxNrOfThreads, value); }
        }
        private int maxNrOfThreads = SettingsWrapper.OptionMaxNrOfThreadsDefaultValue;
    }

}