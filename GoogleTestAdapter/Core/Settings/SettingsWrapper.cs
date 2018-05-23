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

    public class SettingsWrapper
    {
        private readonly object _lock = new object();

        private static readonly string[] NotPrintedProperties =
        {
            nameof(RegexTraitParser),
            nameof(DebuggingNamedPipeId)
        };

        private static readonly PropertyInfo[] PropertiesToPrint = typeof(SettingsWrapper)
            .GetProperties()
            .Where(pi => !NotPrintedProperties.Contains(pi.Name))
            .OrderBy(p => p.Name)
            .ToArray();

        private readonly IGoogleTestAdapterSettingsContainer _settingsContainer;
        private readonly ITestPropertySettingsContainer _testPropertySettingsContainer;
        public RegexTraitParser RegexTraitParser { private get; set; }

        private int _nrOfRunningExecutions;
        private string _currentExecutable;
        private Thread _currentThread;
        private IGoogleTestAdapterSettings _currentSettings;

        public SettingsWrapper(IGoogleTestAdapterSettingsContainer settingsContainer)
            : this(settingsContainer, null)
        {
        }

        public ITestPropertySettingsContainer TestPropertySettingsContainer => _testPropertySettingsContainer;

        public SettingsWrapper(IGoogleTestAdapterSettingsContainer settingsContainer, ITestPropertySettingsContainer testPropertySettingsContainer)
        {
            _settingsContainer = settingsContainer;
            _testPropertySettingsContainer = testPropertySettingsContainer;
            _currentSettings = _settingsContainer.SolutionSettings;
        }

        public virtual SettingsWrapper Clone()
        {
            return new SettingsWrapper(_settingsContainer, _testPropertySettingsContainer) { RegexTraitParser = RegexTraitParser };
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

        public string GetPathExtension(string executable)
            => ReplacePlaceholders(PathExtension, executable);

        public string GetUserParameters(string solutionDirectory, string testDirectory, int threadId)
            => ReplacePlaceholders(AdditionalTestExecutionParam, solutionDirectory, testDirectory, threadId);

        public string GetBatchForTestSetup(string solutionDirectory, string testDirectory, int threadId)
            => ReplacePlaceholders(BatchForTestSetup, solutionDirectory, testDirectory, threadId);

        public string GetBatchForTestTeardown(string solutionDirectory, string testDirectory, int threadId)
            => ReplacePlaceholders(BatchForTestTeardown, solutionDirectory, testDirectory, threadId);

        public string GetWorkingDir(string solutionDirectory, string testDirectory, int threadId)
            => ReplacePlaceholders(WorkingDir, solutionDirectory, testDirectory, threadId);


        private string ReplacePlaceholders(string theString, string solutionDirectory, string testDirectory, int threadId)
        {
            if (string.IsNullOrEmpty(theString))
            {
                return "";
            }

            string result = theString.Replace(TestDirPlaceholder, testDirectory);
            result = result.Replace(ThreadIdPlaceholder, threadId.ToString());
            result = result.Replace(SolutionDirPlaceholder, solutionDirectory);
            return result;
        }

        public static string ReplacePlaceholders(string userParameters, string executable)
        {
            if (string.IsNullOrEmpty(userParameters))
                return "";

            // ReSharper disable once PossibleNullReferenceException
            string executableDir = new FileInfo(executable).Directory.FullName;
            return userParameters
                .Replace(ExecutableDirPlaceholder, executableDir)
                .Replace(ExecutablePlaceholder, executable);
        }


        public static readonly string CategoryTestExecutionName = Resources.CategoryTestExecutionName;
        public static readonly string CategoryTraitsName = Resources.CategoryTraitsName;
        public static readonly string CategoryRuntimeBehaviorName = Resources.CategoryRuntimeBehaviorName;
        public static readonly string CategoryParallelizationName = Resources.CategoryParallelizationName;
        public static readonly string CategoryMiscName = Resources.CategoryMiscName;

        public const string SolutionDirPlaceholder = "$(SolutionDir)";
        public const string TestDirPlaceholder = "$(TestDir)";
        public const string ThreadIdPlaceholder = "$(ThreadId)";
        public const string ExecutablePlaceholder = "$(Executable)";
        public const string ExecutableDirPlaceholder = "$(ExecutableDir)";

        private static readonly string DescriptionOfSolutionDirPlaceHolder =
            string.Format(Resources.DescriptionOfSolutionDirPlaceHolder, SolutionDirPlaceholder);

        private static readonly string DescriptionOfExecutableDirPlaceHolder =
            string.Format(Resources.DescriptionOfExecutableDirPlaceHolder, ExecutableDirPlaceholder);

        private static readonly string DescriptionOfPlaceholdersForBatches =
            string.Format(Resources.DescriptionOfPlaceholdersForBatches, TestDirPlaceholder, ThreadIdPlaceholder) +
            "\n" + DescriptionOfSolutionDirPlaceHolder;

        private static readonly string DescriptionOfPlaceholdersForExecutables =
            DescriptionOfPlaceholdersForBatches + "\n" +
            string.Format(Resources.DescriptionOfPlaceholdersForExecutables, ExecutablePlaceholder) +
            "\n" + DescriptionOfExecutableDirPlaceHolder;

        #region GeneralOptionsPage

        public virtual string DebuggingNamedPipeId => _currentSettings.DebuggingNamedPipeId;

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


        public static readonly string OptionTestDiscoveryTimeoutInSeconds = Resources.OptionTestDiscoveryTimeoutInSeconds;
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


        public static readonly string OptionWorkingDir = Resources.OptionWorkingDir;
        public const string OptionWorkingDirDefaultValue = ExecutableDirPlaceholder;
        public static readonly string OptionWorkingDirDescription =
            string.Format(Resources.OptionWorkingDirDescription, DescriptionOfExecutableDirPlaceHolder, SolutionDirPlaceholder) +
            "\n" + DescriptionOfExecutableDirPlaceHolder + "\n" + DescriptionOfSolutionDirPlaceHolder;

        public virtual string WorkingDir => _currentSettings.WorkingDir ?? OptionWorkingDirDefaultValue;


        public static readonly string OptionPathExtension = Resources.OptionPathExtension;
        public const string OptionPathExtensionDefaultValue = "";
        public static readonly string OptionPathExtensionDescription =
            string.Format(Resources.OptionPathExtensionDescription, ExecutableDirPlaceholder) +
            "\n" + DescriptionOfExecutableDirPlaceHolder;

        public virtual string PathExtension => _currentSettings.PathExtension ?? OptionPathExtensionDefaultValue;


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
        public static readonly string OptionAdditionalTestExecutionParamsDescription =
            Resources.OptionAdditionalTestExecutionParamsDescription +
            "\n" + DescriptionOfPlaceholdersForExecutables;

        public virtual string AdditionalTestExecutionParam => _currentSettings.AdditionalTestExecutionParam ?? OptionAdditionalTestExecutionParamsDefaultValue;


        public static readonly string OptionBatchForTestSetup = Resources.OptionBatchForTestSetup;
        public const string OptionBatchForTestSetupDefaultValue = "";
        public static readonly string OptionBatchForTestSetupDescription =
            Resources.OptionBatchForTestSetupDescription +
            "\n" + DescriptionOfPlaceholdersForBatches;

        public virtual string BatchForTestSetup => _currentSettings.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;


        public static readonly string OptionBatchForTestTeardown = Resources.OptionBatchForTestTeardown;
        public const string OptionBatchForTestTeardownDefaultValue = "";
        public static readonly string OptionBatchForTestTeardownDescription =
            Resources.OptionBatchForTestTeardownDescription +
            "\n" + DescriptionOfPlaceholdersForBatches;

        public virtual string BatchForTestTeardown => _currentSettings.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;


        public static readonly string OptionKillProcessesOnCancel = Resources.OptionKillProcessesOnCancel;
        public const bool OptionKillProcessesOnCancelDefaultValue = false;
        public static readonly string OptionKillProcessesOnCancelDescription = Resources.OptionKillProcessesOnCancelDescription;

        public virtual bool KillProcessesOnCancel => _currentSettings.KillProcessesOnCancel ?? OptionKillProcessesOnCancelDefaultValue;

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