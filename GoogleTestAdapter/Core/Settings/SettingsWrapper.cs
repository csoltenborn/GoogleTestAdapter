// This file has been modified by Microsoft on 5/2018.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Settings
{
    public class RegexTraitPair
    {
        public string Regex { get; }
        public Trait Trait { get; }

        public RegexTraitPair(string regex, string name, string value)
        {
            Regex = regex;
            Trait = new Trait(name, value);
        }

        public override string ToString()
        {
            return $"'{Regex}': {Trait}";
        }
    }

    // TODO localization is messed up, see parent commits
    public class SettingsWrapper
    {
        private const string DescriptionTestExecutionOnly = " (test execution only)";

        private readonly object _lock = new object();

        private static readonly string[] NotPrintedProperties =
        {
            nameof(RegexTraitParser),
            nameof(DebuggingNamedPipeId),
            nameof(SolutionDir)
        };

        private static readonly PropertyInfo[] PropertiesToPrint = typeof(SettingsWrapper)
            .GetProperties()
            .Where(pi => !NotPrintedProperties.Contains(pi.Name))
            .OrderBy(p => p.Name)
            .ToArray();

        private readonly IGoogleTestAdapterSettingsContainer _settingsContainer;
        private readonly ITestPropertySettingsContainer _testPropertySettingsContainer;
        private readonly string _solutionDir;
        public RegexTraitParser RegexTraitParser { private get; set; }

        private int _nrOfRunningExecutions;
        private string _currentExecutable;
        private Thread _currentThread;
        private IGoogleTestAdapterSettings _currentSettings;

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
        }

        public virtual SettingsWrapper Clone()
        {
            return new SettingsWrapper(_settingsContainer, _testPropertySettingsContainer, _solutionDir) { RegexTraitParser = RegexTraitParser };
        }

        // needed for mocking
        // ReSharper disable once UnusedMember.Global
        public SettingsWrapper() { }

        public void ExecuteWithSettingsForExecutable(string executable, Action action, ILogger logger)
        {
            lock (_lock)
            {
                CheckCorrectUsage(executable);

                _nrOfRunningExecutions++;
                if (_nrOfRunningExecutions == 1)
                {
                    _currentExecutable = executable;
                    _currentThread = Thread.CurrentThread;

                    var projectSettings = _settingsContainer.GetSettingsForExecutable(executable);
                    if (projectSettings != null)
                    {
                        _currentSettings = projectSettings;
                        string settingsString = ToString();
                        _currentSettings = _settingsContainer.SolutionSettings;
                        logger.DebugInfo(String.Format(Resources.SettingsMessage, executable, settingsString));

                        _currentSettings = projectSettings;
                    }
                    else
                    {
                        logger.DebugInfo(String.Format(Resources.NoSettingConfigured, executable, this));
                    }
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
                        _currentExecutable = null;
                        _currentThread = null;
                        if (_currentSettings != _settingsContainer.SolutionSettings)
                        {
                            _currentSettings = _settingsContainer.SolutionSettings;
                            logger.DebugInfo(String.Format(Resources.RestoringSolutionSettings, this));
                        }
                    }
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

        public override string ToString()
        {
            return string.Join(", ", PropertiesToPrint.Select(ToString));
        }

        private string ToString(PropertyInfo propertyInfo)
        {
            var value = propertyInfo.GetValue(this);
            if (value is string)
                return $"{propertyInfo.Name}: '{value}'";

            var pairs = value as IEnumerable<RegexTraitPair>;
            if (pairs != null)
                return $"{propertyInfo.Name}: {{{string.Join(", ", pairs)}}}";

            return $"{propertyInfo.Name}: {value}";
        }


        public const string SolutionDirPlaceholder = "$(SolutionDir)";
        private const string DescriptionOfSolutionDirPlaceHolder =
            SolutionDirPlaceholder + " - directory of the solution (only available inside VS)";

        private string ReplaceSolutionDirPlaceholder(string theString)
        {
            if (string.IsNullOrWhiteSpace(theString))
            {
                return "";
            }

            return string.IsNullOrWhiteSpace(SolutionDir) 
                ? theString.Replace(SolutionDirPlaceholder, "")
                : theString.Replace(SolutionDirPlaceholder, SolutionDir);
        }

        
        public const string ExecutablePlaceholder = "$(Executable)";
        private const string DescriptionOfExecutablePlaceHolder =
            ExecutablePlaceholder + " - executable containing the tests";

        public const string ExecutableDirPlaceholder = "$(ExecutableDir)";
        private const string DescriptionOfExecutableDirPlaceHolder =
            ExecutableDirPlaceholder + " - directory containing the test executable";

        private string ReplaceExecutablePlaceholders(string theString, string executable)
        {
            if (string.IsNullOrWhiteSpace(theString))
            {
                return "";
            }

            // ReSharper disable once PossibleNullReferenceException
            string executableDir = new FileInfo(executable).Directory.FullName;
            return theString
                .Replace(ExecutableDirPlaceholder, executableDir)
                .Replace(ExecutablePlaceholder, executable);
        }

        
        public const string TestDirPlaceholder = "$(TestDir)";
        private const string DescriptionOfTestDirPlaceholder =
            TestDirPlaceholder + " - path of a directory which can be used by the tests";

        public const string ThreadIdPlaceholder = "$(ThreadId)";
        private const string DescriptionOfThreadIdPlaceholder =
            ThreadIdPlaceholder + " - id of thread executing the current tests";

        private string ReplaceTestDirAndThreadIdPlaceholders(string theString, string testDirectory, string threadId)
        {
            if (string.IsNullOrWhiteSpace(theString))
            {
                return "";
            }

            return theString
                .Replace(TestDirPlaceholder, testDirectory)
                .Replace(ThreadIdPlaceholder, threadId);
        }

        private string ReplaceTestDirAndThreadIdPlaceholders(string theString, string testDirectory, int threadId)
        {
            return ReplaceTestDirAndThreadIdPlaceholders(theString, testDirectory, threadId.ToString());
        }

        private string RemoveTestDirAndThreadIdPlaceholders(string theString)
        {
            return ReplaceTestDirAndThreadIdPlaceholders(theString, "", "");
        }


        private const string DescriptionOfEnvVarPlaceholders = "Environment variables are also possible, e.g. %PATH%";

        private string ReplaceEnvironmentVariables(string theString)
        {
            if (string.IsNullOrWhiteSpace(theString))
            {
                return "";
            }

            return Environment.ExpandEnvironmentVariables(theString);
        }


        public const string PageGeneralName = "General";
        public const string PageParallelizationName = "Parallelization";
        public const string PageGoogleTestName = "Google Test";

        public static readonly string CategoryTestExecutionName = Resources.CategoryTestExecutionName;
        public static readonly string CategoryTraitsName = Resources.CategoryTraitsName;
        public static readonly string CategoryRuntimeBehaviorName = Resources.CategoryRuntimeBehaviorName;
        public static readonly string CategoryParallelizationName = Resources.CategoryParallelizationName;
        public static readonly string CategoryMiscName = Resources.CategoryMiscName;


        #region GeneralOptionsPage

        public virtual string DebuggingNamedPipeId => _currentSettings.DebuggingNamedPipeId;
        public virtual string SolutionDir => _solutionDir ?? _currentSettings.SolutionDir;

        public static readonly string OptionUseNewTestExecutionFramework = Resources.OptionUseNewTestExecutionFramework;
        public const bool OptionUseNewTestExecutionFrameworkDefaultValue = true;
        public static readonly string OptionUseNewTestExecutionFrameworkDescription = Resources.OptionUseNewTestExecutionFrameworkDescription;

        public virtual bool UseNewTestExecutionFramework => _currentSettings.UseNewTestExecutionFramework ?? OptionUseNewTestExecutionFrameworkDefaultValue;


        public static readonly string OptionPrintTestOutput = Resources.OptionPrintTestOutput;
        public const bool OptionPrintTestOutputDefaultValue = false;
        public static readonly string OptionPrintTestOutputDescription = Resources.OptionPrintTestOutputDescription;

        public virtual bool PrintTestOutput => _currentSettings.PrintTestOutput ?? OptionPrintTestOutputDefaultValue;


        public static readonly string OptionTestDiscoveryRegex = Resources.OptionTestDiscoveryRegex;
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public static readonly string OptionTestDiscoveryRegexDescription = string.Format(Resources.OptionTestDiscoveryRegexDescription, String.Empty);

        public virtual string TestDiscoveryRegex => _currentSettings.TestDiscoveryRegex ?? OptionTestDiscoveryRegexDefaultValue;


        public const string OptionAdditionalPdbs = "Additional PDBs";
        public const string OptionAdditionalPdbsDefaultValue = "";

        public const string OptionAdditionalPdbsDescription =
            "Files matching the provided file patterns are scanned for additional source locations. This can be useful if the PDBs containing the necessary information can not be found by scanning the executables.\n" +
            "File part of each pattern may contain '*' and '?'; patterns are separated by ';'. Example: " + ExecutableDirPlaceholder + "\\pdbs\\*.pdb\n" +
            "Placeholders:\n" + 
            DescriptionOfSolutionDirPlaceHolder + "\n" + 
            DescriptionOfExecutableDirPlaceHolder + "\n" + 
            DescriptionOfExecutablePlaceHolder + "\n" + 
            DescriptionOfEnvVarPlaceholders;

        public virtual string AdditionalPdbs => _currentSettings.AdditionalPdbs ?? OptionAdditionalPdbsDefaultValue;

        public IEnumerable<string> GetAdditionalPdbs(string executable)
            => Utils.SplitAdditionalPdbs(AdditionalPdbs)
                .Select(p => 
                    ReplaceEnvironmentVariables(
                        ReplaceSolutionDirPlaceholder(
                            ReplaceExecutablePlaceholders(p.Trim(), executable))));


        public const string OptionTestDiscoveryTimeoutInSeconds = "Test discovery timeout in s";
        public const int OptionTestDiscoveryTimeoutInSecondsDefaultValue = 30;
        public static readonly string OptionTestDiscoveryTimeoutInSecondsDescription = Resources.OptionTestDiscoveryTimeoutInSecondsDescription;

        public virtual int TestDiscoveryTimeoutInSeconds {
            get
            {
                int timeout = _currentSettings.TestDiscoveryTimeoutInSeconds ?? OptionTestDiscoveryTimeoutInSecondsDefaultValue;
                if (timeout < 0)
                    timeout = OptionTestDiscoveryTimeoutInSecondsDefaultValue;

                return timeout == 0 ? int.MaxValue : timeout;
            }
        }

        public const string OptionWorkingDir = "Working directory";
        public const string OptionWorkingDirDefaultValue = ExecutableDirPlaceholder;
        public const string OptionWorkingDirDescription =
            "If non-empty, will set the working directory for running the tests (default: " + DescriptionOfExecutableDirPlaceHolder + ").\nExample: " + SolutionDirPlaceholder + "\\MyTestDir\nPlaceholders:\n" + 
            DescriptionOfSolutionDirPlaceHolder + "\n" + 
            DescriptionOfExecutableDirPlaceHolder + "\n" + 
            DescriptionOfExecutablePlaceHolder + "\n" + 
            DescriptionOfTestDirPlaceholder + DescriptionTestExecutionOnly + "\n" + 
            DescriptionOfThreadIdPlaceholder + DescriptionTestExecutionOnly + "\n" + 
            DescriptionOfEnvVarPlaceholders;

        public virtual string WorkingDir => string.IsNullOrWhiteSpace(_currentSettings.WorkingDir) 
            ? OptionWorkingDirDefaultValue 
            : _currentSettings.WorkingDir;

        public string GetWorkingDirForExecution(string executable, string testDirectory, int threadId)
        {
            return ReplaceEnvironmentVariables(
                    ReplaceSolutionDirPlaceholder(
                        ReplaceExecutablePlaceholders(
                            ReplaceTestDirAndThreadIdPlaceholders(WorkingDir, testDirectory, threadId), executable)));
        }

        public string GetWorkingDirForDiscovery(string executable)
        {
            return ReplaceEnvironmentVariables(
                    ReplaceSolutionDirPlaceholder(
                        RemoveTestDirAndThreadIdPlaceholders(
                            ReplaceExecutablePlaceholders(WorkingDir, executable))));
        }


        public static readonly string OptionPathExtension = Resources.OptionPathExtension;
        public const string OptionPathExtensionDefaultValue = "";
        public const string OptionPathExtensionDescription =
            "If non-empty, the content will be appended to the PATH variable of the test execution and discovery processes.\nExample: C:\\MyBins;" + ExecutableDirPlaceholder + "\\MyOtherBins;\nPlaceholders:\n" + 
            DescriptionOfSolutionDirPlaceHolder + "\n" + 
            DescriptionOfExecutableDirPlaceHolder + "\n" + 
            DescriptionOfExecutablePlaceHolder + "\n" + 
            DescriptionOfEnvVarPlaceholders;

        public virtual string PathExtension => _currentSettings.PathExtension ?? OptionPathExtensionDefaultValue;

        public string GetPathExtension(string executable)
            => ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplaceExecutablePlaceholders(PathExtension, executable)));


        public const string TraitsRegexesPairSeparator = "//||//";
        public const string TraitsRegexesRegexSeparator = "///";
        public const string TraitsRegexesTraitSeparator = ",";
        public const string OptionTraitsRegexesDefaultValue = "";
        public static readonly string OptionTraitsDescription = string.Format(
            Resources.OptionTraitsDescription,
            TraitsRegexesRegexSeparator,
            TraitsRegexesTraitSeparator,
            TraitsRegexesPairSeparator);

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

        public static readonly string OptionDebugMode = Resources.OptionDebugMode;
        public const bool OptionDebugModeDefaultValue = false;
        public static readonly string OptionDebugModeDescription = Resources.OptionDebugModeDescription;

        public virtual bool DebugMode => _currentSettings.DebugMode ?? OptionDebugModeDefaultValue;


        public static readonly string OptionTimestampOutput = Resources.OptionTimestampOutput;
        public const bool OptionTimestampOutputDefaultValue = false;
        public static readonly string OptionTimestampOutputDescription = Resources.OptionTimestampOutputDescription;

        public virtual bool TimestampOutput => _currentSettings.TimestampOutput ?? OptionTimestampOutputDefaultValue;


        public static readonly string OptionShowReleaseNotes = Resources.OptionShowReleaseNotes;
        public const bool OptionShowReleaseNotesDefaultValue = true;
        public static readonly string OptionShowReleaseNotesDescription = Resources.OptionShowReleaseNotesDescription;

        public virtual bool ShowReleaseNotes => _currentSettings.ShowReleaseNotes ?? OptionShowReleaseNotesDefaultValue;


        public static readonly string OptionAdditionalTestExecutionParams = Resources.OptionAdditionalTestExecutionParams;
        public const string OptionAdditionalTestExecutionParamsDefaultValue = "";
        public const string OptionAdditionalTestExecutionParamsDescription =
            "Additional parameters for Google Test executable during test execution. Placeholders:\n" + 
            DescriptionOfSolutionDirPlaceHolder + "\n" + 
            DescriptionOfExecutableDirPlaceHolder + "\n" + 
            DescriptionOfExecutablePlaceHolder + "\n" + 
            DescriptionOfTestDirPlaceholder + DescriptionTestExecutionOnly + "\n" + 
            DescriptionOfThreadIdPlaceholder + DescriptionTestExecutionOnly + "\n" + 
            DescriptionOfEnvVarPlaceholders;

        public virtual string AdditionalTestExecutionParam => _currentSettings.AdditionalTestExecutionParam ?? OptionAdditionalTestExecutionParamsDefaultValue;

        public string GetUserParametersForExecution(string executable, string testDirectory, int threadId)
            => ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplaceExecutablePlaceholders(
                        ReplaceTestDirAndThreadIdPlaceholders(AdditionalTestExecutionParam, testDirectory, threadId), executable)));

        public string GetUserParametersForDiscovery(string executable)
            => ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    RemoveTestDirAndThreadIdPlaceholders(
                        ReplaceExecutablePlaceholders(AdditionalTestExecutionParam, executable))));


        private const string DescriptionOfPlaceholdersForBatches =
            DescriptionOfSolutionDirPlaceHolder + "\n" + 
            DescriptionOfTestDirPlaceholder + "\n" + 
            DescriptionOfThreadIdPlaceholder + "\n" + 
            DescriptionOfEnvVarPlaceholders;

        public static readonly string OptionBatchForTestSetup = Resources.OptionBatchForTestSetup;
        public const string OptionBatchForTestSetupDefaultValue = "";

        public const string OptionBatchForTestSetupDescription =
            "Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n" +
            DescriptionOfPlaceholdersForBatches;

        public virtual string BatchForTestSetup => _currentSettings.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;

        public string GetBatchForTestSetup(string testDirectory, int threadId)
            => ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplaceTestDirAndThreadIdPlaceholders(BatchForTestSetup, testDirectory, threadId)));


        public static readonly string OptionBatchForTestTeardown = Resources.OptionBatchForTestTeardown;
        public const string OptionBatchForTestTeardownDefaultValue = "";
        public const string OptionBatchForTestTeardownDescription =
            "Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n" + 
            DescriptionOfPlaceholdersForBatches;

        public virtual string BatchForTestTeardown => _currentSettings.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;

        public string GetBatchForTestTeardown(string testDirectory, int threadId)
            => ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplaceTestDirAndThreadIdPlaceholders(BatchForTestTeardown, testDirectory, threadId)));


        public static readonly string OptionKillProcessesOnCancel = Resources.OptionKillProcessesOnCancel;
        public const bool OptionKillProcessesOnCancelDefaultValue = false;
        public static readonly string OptionKillProcessesOnCancelDescription = Resources.OptionKillProcessesOnCancelDescription;

        public virtual bool KillProcessesOnCancel => _currentSettings.KillProcessesOnCancel ?? OptionKillProcessesOnCancelDefaultValue;


        public const string OptionSkipOriginCheck = "Skip check of file origin";
        public const bool OptionSkipOriginCheckDefaultValue = false;
        public const string OptionSkipOriginCheckDescription =
            "If true, it will not be checked whether executables originate from this computer. Note that this might impose security risks, e.g. when building downloaded solutions. This setting can only be changed via VS Options.";

        public virtual bool SkipOriginCheck => _currentSettings.SkipOriginCheck ?? OptionSkipOriginCheckDefaultValue;

        #endregion

        #region ParallelizationOptionsPage

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

        #region GoogleTestOptionsPage

        public static readonly string OptionCatchExceptions = Resources.OptionCatchExceptions;
        public const bool OptionCatchExceptionsDefaultValue = true;
        public static readonly string OptionCatchExceptionsDescription =
            string.Format(Resources.OptionCatchExceptionsDescription, GoogleTestConstants.CatchExceptions);

        public virtual bool CatchExceptions => _currentSettings.CatchExceptions ?? OptionCatchExceptionsDefaultValue;


        public static readonly string OptionBreakOnFailure = Resources.OptionBreakOnFailure;
        public const bool OptionBreakOnFailureDefaultValue = false;
        public static readonly string OptionBreakOnFailureDescription =
            string.Format(Resources.OptionBreakOnFailureDescription, GoogleTestConstants.BreakOnFailure);

        public virtual bool BreakOnFailure => _currentSettings.BreakOnFailure ?? OptionBreakOnFailureDefaultValue;


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

    }

}