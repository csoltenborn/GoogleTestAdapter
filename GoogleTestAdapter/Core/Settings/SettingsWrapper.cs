// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution;

namespace GoogleTestAdapter.Settings
{
    public class SettingsWrapper
    {
        private readonly object _lock = new object();

        private readonly IGoogleTestAdapterSettingsContainer _settingsContainer;
        private readonly ITestPropertySettingsContainer _testPropertySettingsContainer;
        private readonly string _solutionDir;
        private readonly SettingsPrinter _settingsPrinter;

        public RegexTraitParser RegexTraitParser { private get; set; }

        private HelperFilesCache _cache;
        public HelperFilesCache HelperFilesCache
        {
            private get { return _cache; }
            set
            {
                _cache = value ?? throw new ArgumentNullException(nameof(HelperFilesCache));
                _placeholderReplacer = new PlaceholderReplacer(() => SolutionDir, () => _currentSettings, HelperFilesCache, HelperFilesCache.Logger);
            }
        }

        private PlaceholderReplacer _placeholderReplacer;

        private int _nrOfRunningExecutions;
        private string _currentExecutable;
        private Thread _currentThread;
        private IGoogleTestAdapterSettings _currentSettings;

        // needed for mocking
        // ReSharper disable once UnusedMember.Global
        public SettingsWrapper() { }

        public SettingsWrapper(IGoogleTestAdapterSettingsContainer settingsContainer, string solutionDir = null)
            : this(settingsContainer, null, solutionDir)
        {
        }

        public ITestPropertySettingsContainer TestPropertySettingsContainer => _testPropertySettingsContainer;

        public SettingsWrapper(IGoogleTestAdapterSettingsContainer settingsContainer, ITestPropertySettingsContainer testPropertySettingsContainer, string solutionDir)
        {
            _settingsContainer = settingsContainer;
            _testPropertySettingsContainer = testPropertySettingsContainer;
            _solutionDir = solutionDir;
            _currentSettings = _settingsContainer.SolutionSettings;
            _settingsPrinter = new SettingsPrinter(this);
        }

        public virtual SettingsWrapper Clone()
        {
            return new SettingsWrapper(_settingsContainer, _testPropertySettingsContainer, _solutionDir)
            {
                RegexTraitParser = RegexTraitParser,
                HelperFilesCache = HelperFilesCache
            };
        }

        public override string ToString()
        {
            return _settingsPrinter.ToReadableString();
        }

        #region Handling of execution settings

        public void ExecuteWithSettingsForExecutable(string executable, ILogger logger, Action action)
        {
            lock (_lock)
            {
                CheckCorrectUsage(executable);

                _nrOfRunningExecutions++;
                if (_nrOfRunningExecutions == 1)
                {
                    EnableSettingsForExecutable();
                }
            }
            try
            {
                action.Invoke();
            }
            finally
            {
                lock (_lock)
                {
                    _nrOfRunningExecutions--;
                    if (_nrOfRunningExecutions == 0)
                    {
                        ReenableSolutionSettings();
                    }
                }
            }

            void EnableSettingsForExecutable()
            {
                _currentExecutable = executable;
                _currentThread = Thread.CurrentThread;

                var projectSettings = _settingsContainer.GetSettingsForExecutable(executable);
                if (projectSettings != null)
                {
                    _currentSettings = projectSettings;
                    string settingsString = ToString();
                    _currentSettings = _settingsContainer.SolutionSettings;
                    logger.DebugInfo($"Test executable '{executable}': Project settings apply, regex: {projectSettings.ProjectRegex}");
                    logger.DebugInfo(String.Format(Resources.SettingsMessage, executable, settingsString));

                    _currentSettings = projectSettings;
                }
                else
                {
                    logger.DebugInfo(String.Format(Resources.NoSettingConfigured, executable, this));
                }
            }

            void ReenableSolutionSettings()
            {
                _currentExecutable = null;
                _currentThread = null;
                if (_currentSettings != _settingsContainer.SolutionSettings)
                {
                    _currentSettings = _settingsContainer.SolutionSettings;
                    logger.DebugInfo(String.Format(Resources.RestoringSolutionSettings, this));
                }
            }

        }

        // public virtual for mocking
        public virtual void CheckCorrectUsage(string executable)
        {
            if (_nrOfRunningExecutions == 0)
                return;
            if (_nrOfRunningExecutions < 0)
                throw new InvalidOperationException(String.Format(Resources.NeverBeZero, nameof(_nrOfRunningExecutions)));

            if (_currentThread != Thread.CurrentThread)
                throw new InvalidOperationException(String.Format(Resources.SettingsWrapperString, _currentThread.Name, Thread.CurrentThread.Name));

            if (executable != _currentExecutable)
                throw new InvalidOperationException(String.Format(Resources.ExecutionString, _currentExecutable, executable));
        }

        #endregion

        #region Page and category names

        public const string PageGeneralName = "General";
        public const string PageParallelizationName = "Parallelization";
        public const string PageGoogleTestName = "Google Test";
        public const string PageTestDiscovery = "Test Discovery";
        public const string PageTestExecution = "Test Execution";

        public static readonly string CategoryTestExecutionName = Resources.CategoryTestExecutionName;
        public static readonly string CategoryTraitsName = Resources.CategoryTraitsName;
        public static readonly string CategoryRuntimeBehaviorName = Resources.CategoryRuntimeBehaviorName;
        public static readonly string CategoryParallelizationName = Resources.CategoryParallelizationName;
        public static readonly string CategoryMiscName = Resources.CategoryMiscName;
        public const string CategoryOutputName = "Output";
        public const string CategorySecurityName = "Security";
        public const string CategoryRunConfigurationName = "Run configuration (also applies to test discovery)";
        public const string CategorySetupAndTeardownName = "Setup and teardown";

        #endregion

        #region GeneralOptionsPage

        public static readonly string OptionPrintTestOutput = Resources.OptionPrintTestOutput;
        public const bool OptionPrintTestOutputDefaultValue = false;
        public static readonly string OptionPrintTestOutputDescription = Resources.OptionPrintTestOutputDescription;

        public virtual bool PrintTestOutput => _currentSettings.PrintTestOutput ?? OptionPrintTestOutputDefaultValue;


        public const string OptionOutputMode = "Output mode";
        public const string OptionOutputModeDescription =
            "Controls the amount of output printed to the Tests Output window";
        public const OutputMode OptionOutputModeDefaultValue = OutputMode.Info;

        public virtual OutputMode OutputMode => _currentSettings.OutputMode ?? OptionOutputModeDefaultValue;


        public static readonly string OptionTimestampOutput = Resources.OptionTimestampOutput;
        public static readonly string OptionTimestampOutputDescription = Resources.OptionTimestampOutputDescription;
        public const TimestampMode OptionTimestampOutputDefaultValue = TimestampMode.Automatic;

        public virtual TimestampMode TimestampMode => _currentSettings.TimestampMode ?? OptionTimestampOutputDefaultValue;


        public const string OptionSeverityMode = "Print severity";
        public const string OptionSeverityModeDescription =
            "Controls whether the messages' severity is added to the output.\n" + 
            SeverityModeConverter.Automatic + ": print severity on VS2013, VS2015\n" +
            SeverityModeConverter.PrintSeverity + ": always print severity\n" +
            SeverityModeConverter.DoNotPrintSeverity + ": never print severity";
        public const SeverityMode OptionSeverityModeDefaultValue = SeverityMode.Automatic;

        public virtual SeverityMode SeverityMode => _currentSettings.SeverityMode ?? OptionSeverityModeDefaultValue;


        public const string OptionSummaryMode = "Print summary";
        public const string OptionSummaryModeDescription =
            "Controls whether a summary of warnings and errors is printed after test discovery/execution.";
        public const SummaryMode OptionSummaryModeDefaultValue = SummaryMode.WarningOrError;

        public virtual SummaryMode SummaryMode => _currentSettings.SummaryMode ?? OptionSummaryModeDefaultValue;


        public const string OptionPrefixOutputWithGta = "Prefix output with [GTA]";
        public const string OptionPrefixOutputWithGtaDescription =
            "Controls whether the prefix [GTA] is added to GTA's output.";
        public const bool OptionPrefixOutputWithGtaDefaultValue = false;

        public virtual bool PrefixOutputWithGta => _currentSettings.PrefixOutputWithGta ?? OptionPrefixOutputWithGtaDefaultValue;


        public const string OptionSkipOriginCheck = "Skip check of file origin";
        public const string OptionSkipOriginCheckDescription =
            "If true, it will not be checked whether executables originate from this computer. Note that this might impose security risks, e.g. when building downloaded solutions. This setting can only be changed via VS Options.";
        public const bool OptionSkipOriginCheckDefaultValue = false;

        public virtual bool SkipOriginCheck => _currentSettings.SkipOriginCheck ?? OptionSkipOriginCheckDefaultValue;


        #endregion

        #region GoogleTestOptionsPage

        public static readonly string OptionCatchExceptions = Resources.OptionCatchExceptions;
        public static readonly string OptionCatchExceptionsDescription =
            string.Format(Resources.OptionCatchExceptionsDescription, GoogleTestConstants.CatchExceptions);
        public const bool OptionCatchExceptionsDefaultValue = true;

        public virtual bool CatchExceptions => _currentSettings.CatchExceptions ?? OptionCatchExceptionsDefaultValue;

        public static readonly string OptionBreakOnFailure = Resources.OptionBreakOnFailure;
        public const bool OptionBreakOnFailureDefaultValue = false;
        public static readonly string OptionBreakOnFailureDescription =
            string.Format(Resources.OptionBreakOnFailureDescription, GoogleTestConstants.BreakOnFailure);

        public virtual bool BreakOnFailure => _currentSettings.BreakOnFailure ?? OptionBreakOnFailureDefaultValue;

        public static readonly string OptionUseNewTestExecutionFramework = Resources.OptionUseNewTestExecutionFramework;
        public const bool OptionUseNewTestExecutionFrameworkDefaultValue = true;
        public static readonly string OptionUseNewTestExecutionFrameworkDescription = Resources.OptionUseNewTestExecutionFrameworkDescription;

        public static readonly string OptionRunDisabledTests = Resources.OptionRunDisabledTests;
        public const bool OptionRunDisabledTestsDefaultValue = false;
        public static readonly string OptionRunDisabledTestsDescription =
            string.Format(Resources.OptionRunDisabledTestsDescription, GoogleTestConstants.AlsoRunDisabledTestsOption);

        public virtual bool RunDisabledTests => _currentSettings.RunDisabledTests ?? OptionRunDisabledTestsDefaultValue;

        public static readonly string OptionNrOfTestRepetitions = Resources.OptionNrOfTestRepetitions;
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;
        public static readonly string OptionNrOfTestRepetitionsDescription =
            string.Format(Resources.OptionNrOfTestRepetitionsDescription, GoogleTestConstants.NrOfRepetitionsOption);

        public virtual int NrOfTestRepetitions
        {
            get
            {
                int nrOfRepetitions = _currentSettings.NrOfTestRepetitions ?? OptionNrOfTestRepetitionsDefaultValue;
                if (nrOfRepetitions == 0 || nrOfRepetitions < -1)
                {
                    nrOfRepetitions = OptionNrOfTestRepetitionsDefaultValue;
                }
                return nrOfRepetitions;
            }
        }
        
        public static readonly string OptionShuffleTests = Resources.OptionShuffleTests;
        public const bool OptionShuffleTestsDefaultValue = false;
        public static readonly string OptionShuffleTestsDescription =
            string.Format(Resources.OptionShuffleTestsDescription, GoogleTestConstants.ShuffleTestsOption);

        public virtual bool ShuffleTests => _currentSettings.ShuffleTests ?? OptionShuffleTestsDefaultValue;


        public static readonly string OptionShuffleTestsSeed = Resources.OptionShuffleTestsSeed;
        public const int OptionShuffleTestsSeedDefaultValue = GoogleTestConstants.ShuffleTestsSeedDefaultValue;
        public static readonly string OptionShuffleTestsSeedDescription = string.Format(
            Resources.OptionShuffleTestsSeedDescription,
            GoogleTestConstants.ShuffleTestsSeedMaxValueAsString,
            Resources.OptionShuffleTests,
            GoogleTestConstants.ShuffleTestsSeedOption);

        public virtual int ShuffleTestsSeed
        {
            get
            {
                int seed = _currentSettings.ShuffleTestsSeed ?? OptionShuffleTestsSeedDefaultValue;
                if (seed < GoogleTestConstants.ShuffleTestsSeedMinValue || seed > GoogleTestConstants.ShuffleTestsSeedMaxValue)
                {
                    seed = OptionShuffleTestsSeedDefaultValue;
                }
                return seed;
            }
        }

        #endregion

        #region TestExecutionOptionsPage

        public const string OptionDebuggerKind = "Debugger engine";
        public const string OptionDebuggerKindDescription =
                DebuggerKindConverter.VsTestFramework + ": Debugger engine as provided by the VsTest framework; no test crash detection, no test output printing, less interactive UI\n" +
                DebuggerKindConverter.Native + ": Debugger engine as provided by VS native API; no restrictions (default)\n" + 
                DebuggerKindConverter.ManagedAndNative + ": Same as '" + DebuggerKindConverter.Native + "', but allows to also debug into managed code";
        public const DebuggerKind OptionDebuggerKindDefaultValue = DebuggerKind.Native;

        public virtual DebuggerKind DebuggerKind => _currentSettings.DebuggerKind ?? OptionDebuggerKindDefaultValue;


        public const string OptionAdditionalPdbs = "Additional PDBs";
        public const string OptionAdditionalPdbsDescription =
            "Files matching the provided file patterns are scanned for additional source locations. This can be useful if the PDBs containing the necessary information can not be found by scanning the executables.\n" +
            "File part of each pattern may contain '*' and '?'; patterns are separated by ';'. Example: " + PlaceholderReplacer.ExecutableDirPlaceholder + "\\pdbs\\*.pdb\n" + PlaceholderReplacer.AdditionalPdbsPlaceholders;
        public const string OptionAdditionalPdbsDefaultValue = "";

        public virtual string AdditionalPdbs => _currentSettings.AdditionalPdbs ?? OptionAdditionalPdbsDefaultValue;
        public IEnumerable<string> GetAdditionalPdbs(string executable)
            => Utils.SplitAdditionalPdbs(AdditionalPdbs)
                .Select(p => _placeholderReplacer.ReplaceAdditionalPdbsPlaceholders(p, executable));

        public const string OptionWorkingDir = "Working directory";
        public const string OptionWorkingDirDescription =
            "If non-empty, will set the working directory for running the tests (default: " + PlaceholderReplacer.DescriptionOfExecutableDirPlaceHolder + ").\nExample: " + PlaceholderReplacer.SolutionDirPlaceholder + "\\MyTestDir\n" + PlaceholderReplacer.WorkingDirPlaceholders;
        public const string OptionWorkingDirDefaultValue = PlaceholderReplacer.ExecutableDirPlaceholder;

        public virtual string WorkingDir => string.IsNullOrWhiteSpace(_currentSettings.WorkingDir) 
            ? OptionWorkingDirDefaultValue 
            : _currentSettings.WorkingDir;

        public string GetWorkingDirForExecution(string executable, string testDirectory, int threadId)
        {
            return _placeholderReplacer.ReplaceWorkingDirPlaceholdersForExecution(WorkingDir, executable, testDirectory, threadId);
        }

        public string GetWorkingDirForDiscovery(string executable)
        {
            return _placeholderReplacer.ReplaceWorkingDirPlaceholdersForDiscovery(WorkingDir, executable);
        }

        public static readonly string OptionPathExtension = Resources.OptionPathExtension;
        public const string OptionPathExtensionDescription =
            "If non-empty, the content will be appended to the PATH variable of the test execution and discovery processes.\nExample: C:\\MyBins;" + PlaceholderReplacer.ExecutableDirPlaceholder + "\\MyOtherBins\n" + PlaceholderReplacer.PathExtensionPlaceholders;
        public const string OptionPathExtensionDefaultValue = "";

        public virtual string PathExtension => _currentSettings.PathExtension ?? OptionPathExtensionDefaultValue;

        public string GetPathExtension(string executable)
            => _placeholderReplacer.ReplacePathExtensionPlaceholders(PathExtension, executable);


        public static readonly string OptionAdditionalTestExecutionParams = Resources.OptionAdditionalTestExecutionParams;

        public static readonly string OptionDebugMode = Resources.OptionDebugMode;
        public const bool OptionDebugModeDefaultValue = false;
        public static readonly string OptionDebugModeDescription = Resources.OptionDebugModeDescription;

        public virtual bool DebugMode => _currentSettings.DebugMode ?? OptionDebugModeDefaultValue;

        public const string OptionAdditionalTestExecutionParamsDescription =
            "Additional parameters for Google Test executable during test execution. " + PlaceholderReplacer.AdditionalTestExecutionParamPlaceholders;
        public const string OptionAdditionalTestExecutionParamsDefaultValue = "";

        public virtual string AdditionalTestExecutionParam => _currentSettings.AdditionalTestExecutionParam ?? OptionAdditionalTestExecutionParamsDefaultValue;

        public string GetUserParametersForExecution(string executable, string testDirectory, int threadId)
            => _placeholderReplacer.ReplaceAdditionalTestExecutionParamPlaceholdersForExecution(
                AdditionalTestExecutionParam, executable, testDirectory, threadId);

        public string GetUserParametersForDiscovery(string executable)
            => _placeholderReplacer.ReplaceAdditionalTestExecutionParamPlaceholdersForDiscovery(
                AdditionalTestExecutionParam, executable);

        public static readonly string OptionBatchForTestSetup = Resources.OptionBatchForTestSetup;

        public const string OptionBatchForTestSetupDefaultValue = "";

        public const string OptionBatchForTestSetupDescription =
            "Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. " + PlaceholderReplacer.BatchesPlaceholders;

        public virtual string BatchForTestSetup => _currentSettings.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;

        public string GetBatchForTestSetup(string testDirectory, int threadId)
            => _placeholderReplacer.ReplaceSetupBatchPlaceholders(BatchForTestSetup, testDirectory, threadId);


        public static readonly string OptionBatchForTestTeardown = Resources.OptionBatchForTestTeardown;
        public const string OptionBatchForTestTeardownDescription =
            "Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. " + PlaceholderReplacer.BatchesPlaceholders;
        public const string OptionBatchForTestTeardownDefaultValue = "";

        public virtual string BatchForTestTeardown => _currentSettings.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;

        public string GetBatchForTestTeardown(string testDirectory, int threadId)
            => _placeholderReplacer.ReplaceTeardownBatchPlaceholders(BatchForTestTeardown, testDirectory,
                threadId);


        public static readonly string OptionKillProcessesOnCancel = Resources.OptionKillProcessesOnCancel;
        public const bool OptionKillProcessesOnCancelDefaultValue = false;
        public static readonly string OptionKillProcessesOnCancelDescription = Resources.OptionKillProcessesOnCancelDescription;

        public virtual bool KillProcessesOnCancel => _currentSettings.KillProcessesOnCancel ?? OptionKillProcessesOnCancelDefaultValue;


        public const string OptionExitCodeTestCase = "Exit code test case";
        public const string OptionExitCodeTestCaseDescription =
            "If non-empty, an additional test case will be generated per test executable that passes if and only if the test executable returns exit code 0.";
        public const string OptionExitCodeTestCaseDefaultValue = "";

        public virtual string ExitCodeTestCase => _currentSettings.ExitCodeTestCase ?? OptionExitCodeTestCaseDefaultValue;


        public static readonly string OptionEnableParallelTestExecution = Resources.OptionEnableParallelTestExecution;
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;
        public static readonly string OptionEnableParallelTestExecutionDescription = Resources.OptionEnableParallelTestExecutionDescription;

        public virtual bool ParallelTestExecution => _currentSettings.ParallelTestExecution ?? OptionEnableParallelTestExecutionDefaultValue;


        public static readonly string OptionMaxNrOfThreads = Resources.OptionMaxNrOfThreads;
        public const int OptionMaxNrOfThreadsDefaultValue = 0;
        public static readonly string OptionMaxNrOfThreadsDescription = Resources.OptionMaxNrOfThreadsDescription;

        public virtual int MaxNrOfThreads
        {
            get
            {
                int result = _currentSettings.MaxNrOfThreads ?? OptionMaxNrOfThreadsDefaultValue;
                if (result <= 0)
                {
                    result = Environment.ProcessorCount;
                }
                return result;
            }
        }

        #endregion

        #region TestDiscoveryOptionsPage

        public static readonly string OptionTestDiscoveryRegex = Resources.OptionTestDiscoveryRegex;
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public static readonly string OptionTestDiscoveryRegexDescription = string.Format(Resources.OptionTestDiscoveryRegexDescription, String.Empty);

        public virtual string TestDiscoveryRegex => _currentSettings.TestDiscoveryRegex ?? OptionTestDiscoveryRegexDefaultValue;


        public const string OptionTestDiscoveryTimeoutInSeconds = "Test discovery timeout in s";
        public static readonly string OptionTestDiscoveryTimeoutInSecondsDescription = Resources.OptionTestDiscoveryTimeoutInSecondsDescription;
        public const int OptionTestDiscoveryTimeoutInSecondsDefaultValue = 30;

        public virtual int TestDiscoveryTimeoutInSeconds {
            get
            {
                int timeout = _currentSettings.TestDiscoveryTimeoutInSeconds ?? OptionTestDiscoveryTimeoutInSecondsDefaultValue;
                if (timeout < 0)
                    timeout = OptionTestDiscoveryTimeoutInSecondsDefaultValue;

                return timeout == 0 ? int.MaxValue : timeout;
            }
        }


        public const string TraitsRegexesPairSeparator = "//||//";
        public const string TraitsRegexesRegexSeparator = "///";
        public const string TraitsRegexesTraitSeparator = ",";
        public static readonly string OptionTraitsDescription = string.Format(
            Resources.OptionTraitsDescription,
            TraitsRegexesRegexSeparator,
            TraitsRegexesTraitSeparator,
            TraitsRegexesPairSeparator);
        public const string OptionTraitsRegexesDefaultValue = "";
        
        public static readonly string OptionTraitsRegexesBefore = Resources.OptionTraitsRegexesBefore;

        public virtual List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = _currentSettings.TraitsRegexesBefore ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }

        public static readonly string OptionTraitsRegexesAfter = Resources.OptionTraitsRegexesAfter;

        public virtual List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = _currentSettings.TraitsRegexesAfter ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }


        public static readonly string OptionTestNameSeparator = Resources.OptionTestNameSeparator;
        public const string OptionTestNameSeparatorDefaultValue = "";
        public static readonly string OptionTestNameSeparatorDescription = Resources.OptionTestNameSeparatorDescription;

        public virtual string TestNameSeparator => _currentSettings.TestNameSeparator ?? OptionTestNameSeparatorDefaultValue;

        public static readonly string OptionParseSymbolInformation = Resources.OptionParseSymbolInformation;
        public const bool OptionParseSymbolInformationDefaultValue = true;
        public static readonly string OptionParseSymbolInformationDescription = Resources.OptionParseSymbolInformationDescription;

        public virtual bool ParseSymbolInformation => _currentSettings.ParseSymbolInformation ?? OptionParseSymbolInformationDefaultValue;


        #endregion

        #region Internal properties

        public virtual string DebuggingNamedPipeId => _currentSettings.DebuggingNamedPipeId;
        public virtual string SolutionDir => _solutionDir ?? _currentSettings.SolutionDir;

        #endregion

    }

}