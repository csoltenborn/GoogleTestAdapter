using System.ComponentModel;
using GoogleTestAdapter;

namespace GoogleTestAdapterVSIX.OptionsPages
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