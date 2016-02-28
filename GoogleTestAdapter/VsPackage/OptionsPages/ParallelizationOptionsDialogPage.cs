using System.ComponentModel;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class ParallelizationOptionsDialogPage : NotifyingDialogPage
    {
        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionEnableParallelTestExecution)]
        [Description(Options.OptionEnableParallelTestExecutionDescription)]
        public bool EnableParallelTestExecution
        {
            get { return enableParallelTestExecution; }
            set { SetAndNotify(ref enableParallelTestExecution, value); }
        }
        private bool enableParallelTestExecution = Options.OptionEnableParallelTestExecutionDefaultValue;

        [Category(Options.CategoryName)]
        [DisplayName(Options.OptionMaxNrOfThreads)]
        [Description(Options.OptionMaxNrOfThreadsDescription)]
        public int MaxNrOfThreads
        {
            get { return maxNrOfThreads; }
            set { SetAndNotify(ref maxNrOfThreads, value); }
        }
        private int maxNrOfThreads = Options.OptionMaxNrOfThreadsDefaultValue;
    }

}