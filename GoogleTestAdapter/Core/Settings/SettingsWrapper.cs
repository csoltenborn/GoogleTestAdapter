// This file has been modified by Microsoft on 7/2017.

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
        private readonly string _solutionDir;
        private readonly SettingsPrinter _settingsPrinter;

        public RegexTraitParser RegexTraitParser { private get; set; }
        public EnvironmentVariablesParser EnvironmentVariablesParser { private get; set; }

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
        {
            _settingsContainer = settingsContainer;
            _solutionDir = solutionDir;
            _currentSettings = _settingsContainer.SolutionSettings;
            _settingsPrinter = new SettingsPrinter(this);
        }

        public virtual SettingsWrapper Clone()
        {
            return new SettingsWrapper(_settingsContainer, _solutionDir)
            {
                RegexTraitParser = RegexTraitParser,
                HelperFilesCache = HelperFilesCache,
                EnvironmentVariablesParser = EnvironmentVariablesParser
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
                    logger.VerboseInfo($"Settings for test executable '{executable}': {settingsString}");

                    _currentSettings = projectSettings;
                }
                else
                {
                    logger.DebugInfo(
                        $"No settings configured for test executable '{executable}'; running with solution settings");
                }
            }

            void ReenableSolutionSettings()
            {
                _currentExecutable = null;
                _currentThread = null;
                if (_currentSettings != _settingsContainer.SolutionSettings)
                {
                    _currentSettings = _settingsContainer.SolutionSettings;
                    logger.DebugInfo($"Back to solution settings");
                }
            }

        }

        // public virtual for mocking
        public virtual void CheckCorrectUsage(string executable)
        {
            if (_nrOfRunningExecutions == 0)
                return;
            if (_nrOfRunningExecutions < 0)
                throw new InvalidOperationException($"{nameof(_nrOfRunningExecutions)} must never be < 0");

            if (_currentThread != Thread.CurrentThread)
                throw new InvalidOperationException(
                    $"SettingsWrapper is already running with settings for an executable on thread '{_currentThread.Name}', can not also be used by thread {Thread.CurrentThread.Name}");

            if (executable != _currentExecutable)
                throw new InvalidOperationException(
                    $"Execution is already running with settings for executable {_currentExecutable}, can not switch to settings for {executable}");
        }

        #endregion

        #region Page and category names

        public const string PageGeneralName = "General";
        public const string PageGoogleTestName = "Google Test";
        public const string PageTestDiscovery = "Test Discovery";
        public const string PageTestExecution = "Test Execution";

        public const string CategoryTestExecutionName = "Test execution";
        public const string CategoryTraitsName = "Regexes for trait assignment";
        public const string CategoryRuntimeBehaviorName = "Runtime behavior";
        public const string CategoryParallelizationName = "Parallelization";
        public const string CategoryMiscName = "Misc";
        public const string CategoryOutputName = "Output";
        public const string CategorySecurityName = "Security";
        public const string CategoryRunConfigurationName = "Run configuration (also applies to test discovery)";
        public const string CategorySetupAndTeardownName = "Setup and teardown";

        #endregion

        #region GeneralOptionsPage

        public const string OptionPrintTestOutput = "Print test output";
        public const string OptionPrintTestOutputDescription = 
            "Print the output of the Google Test executable(s) to the Tests Output window.";
        public const bool OptionPrintTestOutputDefaultValue = false;

        public virtual bool PrintTestOutput => _currentSettings.PrintTestOutput ?? OptionPrintTestOutputDefaultValue;


        public const string OptionOutputMode = "Output mode";
        public const string OptionOutputModeDescription =
            "Controls the amount of output printed to the Tests Output window";
        public const OutputMode OptionOutputModeDefaultValue = OutputMode.Info;

        public virtual OutputMode OutputMode => _currentSettings.OutputMode ?? OptionOutputModeDefaultValue;


        public const string OptionTimestampOutput = "Timestamp output";
        public const string OptionTimestampOutputDescription =
            "Controls whether a timestamp is added to the output.\n" + 
            TimestampModeConverter.Automatic + ": add timestamp on VS2013, VS2015\n" +
            TimestampModeConverter.PrintTimeStamp + ": always add timestamp\n" + 
            TimestampModeConverter.DoNotPrintTimeStamp + ": never add timestamp";
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

        public const string OptionCatchExceptions = "Catch exceptions";
        public const string OptionCatchExceptionsDescription =
            "Google Test catches exceptions by default; the according test fails and test execution continues. Choosing false lets exceptions pass through, allowing the debugger to catch them.\n"
            + "Google Test option:" + GoogleTestConstants.CatchExceptions;
        public const bool OptionCatchExceptionsDefaultValue = true;

        public virtual bool CatchExceptions => _currentSettings.CatchExceptions ?? OptionCatchExceptionsDefaultValue;


        public const string OptionBreakOnFailure = "Break on failure";
        public const string OptionBreakOnFailureDescription =
            "If enabled, a potentially attached debugger will catch assertion failures and automatically drop into interactive mode.\n"
            + "Google Test option:" + GoogleTestConstants.BreakOnFailure;
        public const bool OptionBreakOnFailureDefaultValue = false;

        public virtual bool BreakOnFailure => _currentSettings.BreakOnFailure ?? OptionBreakOnFailureDefaultValue;


        public const string OptionRunDisabledTests = "Also run disabled tests";
        public const string OptionRunDisabledTestsDescription =
            "If true, all (selected) tests will be run, even if they have been disabled.\n"
            + "Google Test option:" + GoogleTestConstants.AlsoRunDisabledTestsOption;
        public const bool OptionRunDisabledTestsDefaultValue = false;

        public virtual bool RunDisabledTests => _currentSettings.RunDisabledTests ?? OptionRunDisabledTestsDefaultValue;


        public const string OptionNrOfTestRepetitions = "Number of test repetitions";
        public const string OptionNrOfTestRepetitionsDescription =
            "Tests will be run for the selected number of times (-1: infinite).\n"
            + "Google Test option:" + GoogleTestConstants.NrOfRepetitionsOption;
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;

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


        public const string OptionShuffleTests = "Shuffle tests per execution";
        public const string OptionShuffleTestsDescription =
            "If true, tests will be executed in random order. Note that a true randomized order is only given when executing all tests in non-parallel fashion. Otherwise, the test excutables will most likely be executed more than once - random order is then restricted to the according executions.\n"
            + "Google Test option:" + GoogleTestConstants.ShuffleTestsOption;
        public const bool OptionShuffleTestsDefaultValue = false;

        public virtual bool ShuffleTests => _currentSettings.ShuffleTests ?? OptionShuffleTestsDefaultValue;


        public const string OptionShuffleTestsSeed = "Shuffle tests: Seed";
        public const string OptionShuffleTestsSeedDescription = "0: Seed is computed from system time, 1<=n<="
                                                           + GoogleTestConstants.ShuffleTestsSeedMaxValueAsString
                                                           + ": The given seed is used. See note of option '"
                                                           + OptionShuffleTests
                                                           + "'.\n"
            + "Google Test option:" + GoogleTestConstants.ShuffleTestsSeedOption;
        public const int OptionShuffleTestsSeedDefaultValue = GoogleTestConstants.ShuffleTestsSeedDefaultValue;

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

        public const string OptionPathExtension = "PATH extension";
        public const string OptionPathExtensionDescription =
            "If non-empty, the content will be appended to the PATH variable of the test execution and discovery processes.\nExample: C:\\MyBins;" + PlaceholderReplacer.ExecutableDirPlaceholder + "\\MyOtherBins\n" + PlaceholderReplacer.PathExtensionPlaceholders;
        public const string OptionPathExtensionDefaultValue = "";

        public virtual string PathExtension => _currentSettings.PathExtension ?? OptionPathExtensionDefaultValue;

        public string GetPathExtension(string executable)
            => _placeholderReplacer.ReplacePathExtensionPlaceholders(PathExtension, executable);


        public const string OptionEnvironmentVariables = "Environment variables";
        public const string OptionEnvironmentVariablesDescription = "Allows to provide environment variables which will be added to a test executable's run context. Environment variables are separated by '" 
                                                                    + EnvironmentVariablesParser.Separator
                                                                    + "'.\nExample: MyVar=MyValue" + EnvironmentVariablesParser.Separator + "MyDir=" + PlaceholderReplacer.TestDirPlaceholder 
                                                                    + "\n" + PlaceholderReplacer.EnvironmentPlaceholders;
        public const string OptionEnvironmentVariablesDefaultValue = "";

        public virtual string EnvironmentVariables =>
            _currentSettings.EnvironmentVariables ?? OptionEnvironmentVariablesDefaultValue;

        public IDictionary<string, string> GetEnvironmentVariablesForDiscovery(string executable)
            => EnvironmentVariablesParser.ParseEnvironmentVariablesString(
                _placeholderReplacer.ReplaceEnvironmentVariablesPlaceholdersForDiscovery(EnvironmentVariables, executable));

        public IDictionary<string, string> GetEnvironmentVariablesForExecution(string executable, string testDirectory, int threadId) 
            => EnvironmentVariablesParser.ParseEnvironmentVariablesString(
                _placeholderReplacer.ReplaceEnvironmentVariablesPlaceholdersForExecution(EnvironmentVariables, executable, testDirectory, threadId));
        

        public const string OptionAdditionalTestExecutionParams = "Additional test execution parameters";
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


        public const string OptionBatchForTestSetup = "Test setup batch file";
        public const string OptionBatchForTestSetupDefaultValue = "";

        public const string OptionBatchForTestSetupDescription =
            "Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. " + PlaceholderReplacer.BatchesPlaceholders;

        public virtual string BatchForTestSetup => _currentSettings.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;

        public string GetBatchForTestSetup(string testDirectory, int threadId)
            => _placeholderReplacer.ReplaceSetupBatchPlaceholders(BatchForTestSetup, testDirectory, threadId);


        public const string OptionBatchForTestTeardown = "Test teardown batch file";
        public const string OptionBatchForTestTeardownDescription =
            "Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. " + PlaceholderReplacer.BatchesPlaceholders;
        public const string OptionBatchForTestTeardownDefaultValue = "";

        public virtual string BatchForTestTeardown => _currentSettings.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;

        public string GetBatchForTestTeardown(string testDirectory, int threadId)
            => _placeholderReplacer.ReplaceTeardownBatchPlaceholders(BatchForTestTeardown, testDirectory,
                threadId);


        public const string OptionKillProcessesOnCancel = "Kill processes on cancel";
        public const string OptionKillProcessesOnCancelDescription =
            "If true, running test executables are actively killed if the test execution is canceled. Note that killing a test process might have all kinds of side effects; in particular, Google Test will not be able to perform any shutdown tasks.";
        public const bool OptionKillProcessesOnCancelDefaultValue = false;

        public virtual bool KillProcessesOnCancel => _currentSettings.KillProcessesOnCancel ?? OptionKillProcessesOnCancelDefaultValue;


        public const string OptionExitCodeTestCase = "Exit code test case";
        public const string OptionExitCodeTestCaseDescription =
            "If non-empty, an additional test case will be generated per test executable that passes if and only if the test executable returns exit code 0.";
        public const string OptionExitCodeTestCaseDefaultValue = "";

        public virtual string ExitCodeTestCase => _currentSettings.ExitCodeTestCase ?? OptionExitCodeTestCaseDefaultValue;


        public const string OptionEnableParallelTestExecution = "Parallel test execution";
        public const string OptionEnableParallelTestExecutionDescription =
            "Parallel test execution is achieved by means of different threads, each of which is assigned a number of tests to be executed. The threads will then sequentially invoke the necessary executables to produce the according test results.";
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;

        public virtual bool ParallelTestExecution => _currentSettings.ParallelTestExecution ?? OptionEnableParallelTestExecutionDefaultValue;


        public const string OptionMaxNrOfThreads = "Maximum number of threads";
        public const string OptionMaxNrOfThreadsDescription =
            "Maximum number of threads to be used for test execution (0: one thread for each processor).";
        public const int OptionMaxNrOfThreadsDefaultValue = 0;

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

        public const string OptionMissingTestsReportMode = "Behavior for missing test results";
        public const string OptionMissingTestsReportModeDescription =
            "If a test can not be run (e.g. because a dependency has been removed since discovery without VS noticing), this option allows to configure how that test will be reported to the VS test framework." + 
            "\nDefault: " + MissingTestsReportModeConverter.ReportAsNotFound;
        public const MissingTestsReportMode OptionMissingTestsReportModeDefaultValue = MissingTestsReportMode.ReportAsNotFound;

        public virtual MissingTestsReportMode MissingTestsReportMode =>
            _currentSettings.MissingTestsReportMode ?? OptionMissingTestsReportModeDefaultValue;

        #endregion

        #region TestDiscoveryOptionsPage

        public const string OptionTestDiscoveryRegex = "Regex for test discovery";
        public const string OptionTestDiscoveryRegexDescription =
            "If non-empty, only executables matching this regex will be considered as Google Test executables. Note that setting this option will slightly speed up test discovery since the executables do not need to be scanned.";
        public const string OptionTestDiscoveryRegexDefaultValue = "";

        public virtual string TestDiscoveryRegex => _currentSettings.TestDiscoveryRegex ?? OptionTestDiscoveryRegexDefaultValue;


        public const string OptionTestDiscoveryTimeoutInSeconds = "Test discovery timeout in s";
        public const string OptionTestDiscoveryTimeoutInSecondsDescription =
            "Number of seconds after which test discovery of an executable will be assumed to have failed. 0: Infinite timeout";
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
        public const string OptionTraitsDescription = "Allows to override/add traits for testcases matching a regex. Traits are build up in 3 phases: 1st, traits are assigned to tests according to the '" + OptionTraitsRegexesBefore + "' option. 2nd, the tests' traits (defined via the macros in GTA_Traits.h) are added to the tests, overriding traits from phase 1 with new values. 3rd, the '" + OptionTraitsRegexesAfter + "' option is evaluated, again in an overriding manner.\nSyntax: "
                                                 + TraitsRegexesRegexSeparator +
                                                 " separates the regex from the traits, the trait's name and value are separated by "
                                                 + TraitsRegexesTraitSeparator +
                                                 " and each pair of regex and trait is separated by "
                                                 + TraitsRegexesPairSeparator + ".\nExample: " +
                                                 @"MySuite\.*"
                                                 + TraitsRegexesRegexSeparator + "Type"
                                                 + TraitsRegexesTraitSeparator + "Small"
                                                 + TraitsRegexesPairSeparator +
                                                 @"MySuite2\.*|MySuite3\.*"
                                                 + TraitsRegexesRegexSeparator + "Type"
                                                 + TraitsRegexesTraitSeparator + "Medium";
        public const string OptionTraitsRegexesDefaultValue = "";

        public const string OptionTraitsRegexesBefore = "Before test discovery";

        public virtual List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = _currentSettings.TraitsRegexesBefore ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }

        public const string OptionTraitsRegexesAfter = "After test discovery";

        public virtual List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = _currentSettings.TraitsRegexesAfter ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }


        public const string OptionTestNameSeparator = "Test name separator";
        public const string OptionTestNameSeparatorDescription =
            "Test names produced by Google Test might contain the character '/', which makes VS cut the name after the '/' if the test explorer window is not wide enough. This option's value, if non-empty, will replace the '/' character to avoid that behavior. Note that '\\', ' ', '|', and '-' produce the same behavior ('.', '_', ':', and '::' are known to work - there might be more). Note also that traits regexes are evaluated against the tests' display names (and must thus be consistent with this option).";
        public const string OptionTestNameSeparatorDefaultValue = "";

        public virtual string TestNameSeparator => _currentSettings.TestNameSeparator ?? OptionTestNameSeparatorDefaultValue;


        public const string OptionParseSymbolInformation = "Parse symbol information";
        public const string OptionParseSymbolInformationDescription =
            "Parse debug symbol information for test executables. Setting this to false will speed up test discovery, but tests will not have source location information, and traits defined via the macros in GTA_Traits.h will not be available.";
        public const bool OptionParseSymbolInformationDefaultValue = true;

        public virtual bool ParseSymbolInformation => _currentSettings.ParseSymbolInformation ?? OptionParseSymbolInformationDefaultValue;


        #endregion

        #region Internal properties

        public virtual string DebuggingNamedPipeId => _currentSettings.DebuggingNamedPipeId;
        public virtual string SolutionDir => _solutionDir ?? _currentSettings.SolutionDir;

        #endregion

    }

}