// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution;
using Microsoft.VisualStudio.Shell;

// ReSharper disable LocalizableElement

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public class TestExecutionOptionsDialogPage : NotifyingDialogPage
    {
        #region Parallelization

        [LocalizedCategory("CategoryParallelizationName")]
        [LocalizedDisplayName("OptionEnableParallelTestExecution")]
        [LocalizedDescription("OptionEnableParallelTestExecutionDescription")]
        public bool EnableParallelTestExecution
        {
            get => _enableParallelTestExecution;
            set => SetAndNotify(ref _enableParallelTestExecution, value);
        }
        private bool _enableParallelTestExecution = SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue;

        [LocalizedCategory("CategoryParallelizationName")]
        [LocalizedDisplayName("OptionMaxNrOfThreads")]
        [LocalizedDescription("OptionMaxNrOfThreadsDescription")]
        public int MaxNrOfThreads
        {
            get => _maxNrOfThreads;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxNrOfThreads), value, "Expected a number greater than or equal to 0.");
                SetAndNotify(ref _maxNrOfThreads, value);
            }
        }
        private int _maxNrOfThreads = SettingsWrapper.OptionMaxNrOfThreadsDefaultValue;

        #endregion

        #region Run configuration

        [LocalizedCategory("CategoryTestExecutionName")]
        [DisplayName(SettingsWrapper.OptionAdditionalPdbs)]
        [Description(SettingsWrapper.OptionAdditionalPdbsDescription)]
        public string AdditionalPdbs
        {
            get => _additionalPdbs;
            set
            {
                var patterns = Utils.SplitAdditionalPdbs(_additionalPdbs);
                var errorMessages = new List<string>();
                foreach (string pattern in patterns)
                {
                    if (!Utils.ValidatePattern(pattern, out string errorMessage))
                    {
                        errorMessages.Add(errorMessage);
                    }
                }
                if (errorMessages.Any())
                {
                    throw new ArgumentException(string.Join(Environment.NewLine, errorMessages), nameof(AdditionalPdbs));
                }

                SetAndNotify(ref _additionalPdbs, value);
            }
        }
        private string _additionalPdbs = SettingsWrapper.OptionAdditionalPdbsDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionWorkingDir")]
        [LocalizedDescription("OptionWorkingDirDescription")]
        public string WorkingDir
        {
            get => _workingDirectory;
            set => SetAndNotify(ref _workingDirectory, value);
        }
        private string _workingDirectory = SettingsWrapper.OptionWorkingDirDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionPathExtension")]
        [LocalizedDescription("OptionPathExtensionDescription")]
        public string PathExtension
        {
            get => _pathExtension;
            set => SetAndNotify(ref _pathExtension, value);
        }
        private string _pathExtension = SettingsWrapper.OptionPathExtensionDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionAdditionalTestExecutionParams")]
        [LocalizedDescription("OptionAdditionalTestExecutionParamsDescription")]
        public string AdditionalTestExecutionParams
        {
            get => _additionalTestExecutionParams;
            set => SetAndNotify(ref _additionalTestExecutionParams, value);
        }
        private string _additionalTestExecutionParams = SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue;

        #endregion

        #region Setup and teardown

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionBatchForTestSetup")]
        [LocalizedDescription("OptionBatchForTestSetupDescription")]
        public string BatchForTestSetup
        {
            get => _batchForTestSetup;
            set => SetAndNotify(ref _batchForTestSetup, value);
        }
        private string _batchForTestSetup = SettingsWrapper.OptionBatchForTestSetupDefaultValue;

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionBatchForTestTeardown")]
        [LocalizedDescription("OptionBatchForTestTeardownDescription")]
        public string BatchForTestTeardown
        {
            get => _batchForTestTeardown;
            set => SetAndNotify(ref _batchForTestTeardown, value);
        }
        private string _batchForTestTeardown = SettingsWrapper.OptionBatchForTestTeardownDefaultValue;

        #endregion

        #region Misc

        [LocalizedCategory("CategoryTestExecutionName")]
        [LocalizedDisplayName("OptionKillProcessesOnCancel")]
        [LocalizedDescription("OptionKillProcessesOnCancelDescription")]
        public bool KillProcessesOnCancel
        {
            get => _killProcessesOnCancel;
            set => SetAndNotify(ref _killProcessesOnCancel, value);
        }
        private bool _killProcessesOnCancel = SettingsWrapper.OptionKillProcessesOnCancelDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [DisplayName(SettingsWrapper.OptionDebuggerKind)]
        [Description(SettingsWrapper.OptionDebuggerKindDescription)]
        [PropertyPageTypeConverter(typeof(DebuggerKindConverter))]
        public DebuggerKind DebuggerKind
        {
            get => _debuggerKind;
            set => SetAndNotify(ref _debuggerKind, value);
        }
        private DebuggerKind _debuggerKind = SettingsWrapper.OptionDebuggerKindDefaultValue;

        [LocalizedCategory("CategoryMiscName")]
        [DisplayName(SettingsWrapper.OptionExitCodeTestCase)]
        [Description(SettingsWrapper.OptionExitCodeTestCaseDescription)]
        public string ExitCodeTestCase
        {
            get => _exitCodeTestCase;
            set => SetAndNotify(ref _exitCodeTestCase, value);
        }
        private string _exitCodeTestCase = SettingsWrapper.OptionExitCodeTestCaseDefaultValue;

        #endregion

    }

}