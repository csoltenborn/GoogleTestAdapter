// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.Settings;
using System;
using System.ComponentModel;

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
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Expected a number greater than or equal to 0.");
                SetAndNotify(ref _maxNrOfThreads, value);
            }
        }
        private int _maxNrOfThreads = SettingsWrapper.OptionMaxNrOfThreadsDefaultValue;
    }

}