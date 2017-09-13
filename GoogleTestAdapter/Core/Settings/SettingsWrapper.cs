// This file has been modified by Microsoft on 9/2017.

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

        public const string TestFinderRegex = @"[Tt]est[s]?[0-9]*\.exe";

        private readonly IGoogleTestAdapterSettingsContainer _settingsContainer;
        public RegexTraitParser RegexTraitParser { private get; set; }

        private int _nrOfRunningExecutions;
        private string _currentExecutable;
        private Thread _currentThread;
        private IGoogleTestAdapterSettings _currentSettings;

        static SettingsWrapper()
        {
            DescriptionOfPlaceholdersForBatches =
                string.Format(Resources.DescriptionOfPlaceholdersForBatches, TestDirPlaceholder, ThreadIdPlaceholder) +
                "\n" + DescriptionOfSolutionDirPlaceHolder;

            DescriptionOfPlaceholdersForExecutables =
                DescriptionOfPlaceholdersForBatches + "\n" +
                string.Format(Resources.DescriptionOfPlaceholdersForExecutables, ExecutablePlaceholder) +
                "\n" + DescriptionOfExecutableDirPlaceHolder;

            OptionWorkingDirDescription =
                string.Format(Resources.OptionWorkingDirDescription, DescriptionOfExecutableDirPlaceHolder, SolutionDirPlaceholder) +
                "\n" + DescriptionOfExecutableDirPlaceHolder + "\n" + DescriptionOfSolutionDirPlaceHolder;

            OptionPathExtensionDescription =
                string.Format(Resources.OptionPathExtensionDescription, ExecutableDirPlaceholder) +
                "\n" + DescriptionOfExecutableDirPlaceHolder;

            OptionAdditionalTestExecutionParamsDescription =
                Resources.OptionAdditionalTestExecutionParamsDescription +
                "\n" + DescriptionOfPlaceholdersForExecutables;

            OptionBatchForTestSetupDescription =
                Resources.OptionBatchForTestSetupDescription +
                "\n" + DescriptionOfPlaceholdersForBatches;

            OptionBatchForTestTeardownDescription =
                Resources.OptionBatchForTestTeardownDescription +
                "\n" + DescriptionOfPlaceholdersForBatches;
        }

        public SettingsWrapper(IGoogleTestAdapterSettingsContainer settingsContainer)
        {
            _settingsContainer = settingsContainer;
            _currentSettings = _settingsContainer.SolutionSettings;
        }

        public virtual SettingsWrapper Clone()
        {
            return new SettingsWrapper(_settingsContainer) { RegexTraitParser = RegexTraitParser };
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


        public static string CategoryTestExecutionName = Resources.CategoryTestExecutionName;
        public static string CategoryTraitsName = Resources.CategoryTraitsName;
        public static string CategoryRuntimeBehaviorName = Resources.CategoryRuntimeBehaviorName;
        public static string CategoryParallelizationName = Resources.CategoryParallelizationName;
        public static string CategoryMiscName = Resources.CategoryMiscName;

        public const string SolutionDirPlaceholder = "$(SolutionDir)";
        public const string TestDirPlaceholder = "$(TestDir)";
        public const string ThreadIdPlaceholder = "$(ThreadId)";
        public const string ExecutablePlaceholder = "$(Executable)";
        public const string ExecutableDirPlaceholder = "$(ExecutableDir)";

        private static string DescriptionOfSolutionDirPlaceHolder =
            string.Format(Resources.DescriptionOfSolutionDirPlaceHolder, SolutionDirPlaceholder);

        private static string DescriptionOfExecutableDirPlaceHolder =
            string.Format(Resources.DescriptionOfExecutableDirPlaceHolder, ExecutableDirPlaceholder);

        // Set in constructor because it depends on other strings
        private static string DescriptionOfPlaceholdersForBatches;

        // Set in constructor because it depends on other strings
        private static string DescriptionOfPlaceholdersForExecutables;

        #region GeneralOptionsPage

        public virtual string DebuggingNamedPipeId => _currentSettings.DebuggingNamedPipeId;

        public static string OptionUseNewTestExecutionFramework = Resources.OptionUseNewTestExecutionFramework;
        public const bool OptionUseNewTestExecutionFrameworkDefaultValue = true;
        public static string OptionUseNewTestExecutionFrameworkDescription = Resources.OptionUseNewTestExecutionFrameworkDescription;

        public virtual bool UseNewTestExecutionFramework => _currentSettings.UseNewTestExecutionFramework ?? OptionUseNewTestExecutionFrameworkDefaultValue;


        public static string OptionPrintTestOutput = Resources.OptionPrintTestOutput;
        public const bool OptionPrintTestOutputDefaultValue = false;
        public static string OptionPrintTestOutputDescription = Resources.OptionPrintTestOutputDescription;

        public virtual bool PrintTestOutput => _currentSettings.PrintTestOutput ?? OptionPrintTestOutputDefaultValue;


        public static string OptionTestDiscoveryRegex = Resources.OptionTestDiscoveryRegex;
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public static string OptionTestDiscoveryRegexDescription = Resources.OptionTestDiscoveryRegexDescription + TestFinderRegex;

        public virtual string TestDiscoveryRegex => _currentSettings.TestDiscoveryRegex ?? OptionTestDiscoveryRegexDefaultValue;


        public static string OptionTestDiscoveryTimeoutInSeconds = Resources.OptionTestDiscoveryTimeoutInSeconds;
        public const int OptionTestDiscoveryTimeoutInSecondsDefaultValue = 30;
        public static string OptionTestDiscoveryTimeoutInSecondsDescription = Resources.OptionTestDiscoveryTimeoutInSecondsDescription;

        public virtual int TestDiscoveryTimeoutInSeconds {
            get
            {
                int timeout = _currentSettings.TestDiscoveryTimeoutInSeconds ?? OptionTestDiscoveryTimeoutInSecondsDefaultValue;
                if (timeout < 0)
                    timeout = OptionTestDiscoveryTimeoutInSecondsDefaultValue;

                return timeout == 0 ? int.MaxValue : timeout;
            }
        }


        public static string OptionWorkingDir = Resources.OptionWorkingDir;
        public const string OptionWorkingDirDefaultValue = ExecutableDirPlaceholder;
        // Set in constructor because it depends on other strings
        public static string OptionWorkingDirDescription;

        public virtual string WorkingDir => _currentSettings.WorkingDir ?? OptionWorkingDirDefaultValue;


        public static string OptionPathExtension = Resources.OptionPathExtension;
        public const string OptionPathExtensionDefaultValue = "";
        // Set in constructor because it depends on other strings
        public static string OptionPathExtensionDescription;

        public virtual string PathExtension => _currentSettings.PathExtension ?? OptionPathExtensionDefaultValue;


        public const string TraitsRegexesPairSeparator = "//||//";
        public const string TraitsRegexesRegexSeparator = "///";
        public const string TraitsRegexesTraitSeparator = ",";
        public const string OptionTraitsRegexesDefaultValue = "";
        public static string OptionTraitsDescription = string.Format(
            Resources.OptionTraitsDescription,
            TraitsRegexesRegexSeparator,
            TraitsRegexesTraitSeparator,
            TraitsRegexesPairSeparator);

        public static string OptionTraitsRegexesBefore = Resources.OptionTraitsRegexesBefore;

        public virtual List<RegexTraitPair> TraitsRegexesBefore
        {
            get
            {
                string option = _currentSettings.TraitsRegexesBefore ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }

        public static string OptionTraitsRegexesAfter = Resources.OptionTraitsRegexesAfter;

        public virtual List<RegexTraitPair> TraitsRegexesAfter
        {
            get
            {
                string option = _currentSettings.TraitsRegexesAfter ?? OptionTraitsRegexesDefaultValue;
                return RegexTraitParser.ParseTraitsRegexesString(option);
            }
        }


        public static string OptionTestNameSeparator = Resources.OptionTestNameSeparator;
        public const string OptionTestNameSeparatorDefaultValue = "";
        public static string OptionTestNameSeparatorDescription = Resources.OptionTestNameSeparatorDescription;

        public virtual string TestNameSeparator => _currentSettings.TestNameSeparator ?? OptionTestNameSeparatorDefaultValue;


        public static string OptionParseSymbolInformation = Resources.OptionParseSymbolInformation;
        public const bool OptionParseSymbolInformationDefaultValue = true;
        public static string OptionParseSymbolInformationDescription = Resources.OptionParseSymbolInformationDescription;

        public virtual bool ParseSymbolInformation => _currentSettings.ParseSymbolInformation ?? OptionParseSymbolInformationDefaultValue;

        public static string OptionDebugMode = Resources.OptionDebugMode;
        public const bool OptionDebugModeDefaultValue = false;
        public static string OptionDebugModeDescription = Resources.OptionDebugModeDescription;

        public virtual bool DebugMode => _currentSettings.DebugMode ?? OptionDebugModeDefaultValue;


        public static string OptionTimestampOutput = Resources.OptionTimestampOutput;
        public const bool OptionTimestampOutputDefaultValue = false;
        public static string OptionTimestampOutputDescription = Resources.OptionTimestampOutputDescription;

        public virtual bool TimestampOutput => _currentSettings.TimestampOutput ?? OptionTimestampOutputDefaultValue;


        public static string OptionShowReleaseNotes = Resources.OptionShowReleaseNotes;
        public const bool OptionShowReleaseNotesDefaultValue = true;
        public static string OptionShowReleaseNotesDescription = Resources.OptionShowReleaseNotesDescription;

        public virtual bool ShowReleaseNotes => _currentSettings.ShowReleaseNotes ?? OptionShowReleaseNotesDefaultValue;


        public static string OptionAdditionalTestExecutionParams = Resources.OptionAdditionalTestExecutionParams;
        public const string OptionAdditionalTestExecutionParamsDefaultValue = "";
        // Set in constructor because it depends on other strings
        public static string OptionAdditionalTestExecutionParamsDescription;

        public virtual string AdditionalTestExecutionParam => _currentSettings.AdditionalTestExecutionParam ?? OptionAdditionalTestExecutionParamsDefaultValue;


        public static string OptionBatchForTestSetup = Resources.OptionBatchForTestSetup;
        public const string OptionBatchForTestSetupDefaultValue = "";
        // Set in constructor because it depends on other strings
        public static string OptionBatchForTestSetupDescription;

        public virtual string BatchForTestSetup => _currentSettings.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;


        public static string OptionBatchForTestTeardown = Resources.OptionBatchForTestTeardown;
        public const string OptionBatchForTestTeardownDefaultValue = "";
        // Set in constructor because it depends on other strings
        public static string OptionBatchForTestTeardownDescription;

        public virtual string BatchForTestTeardown => _currentSettings.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;


        public static string OptionKillProcessesOnCancel = Resources.OptionKillProcessesOnCancel;
        public const bool OptionKillProcessesOnCancelDefaultValue = false;
        public static string OptionKillProcessesOnCancelDescription = Resources.OptionKillProcessesOnCancelDescription;

        public virtual bool KillProcessesOnCancel => _currentSettings.KillProcessesOnCancel ?? OptionKillProcessesOnCancelDefaultValue;

        #endregion

        #region ParallelizationOptionsPage

        public static string OptionEnableParallelTestExecution = Resources.OptionEnableParallelTestExecution;
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;
        public static string OptionEnableParallelTestExecutionDescription = Resources.OptionEnableParallelTestExecutionDescription;

        public virtual bool ParallelTestExecution => _currentSettings.ParallelTestExecution ?? OptionEnableParallelTestExecutionDefaultValue;


        public static string OptionMaxNrOfThreads = Resources.OptionMaxNrOfThreads;
        public const int OptionMaxNrOfThreadsDefaultValue = 0;
        public static string OptionMaxNrOfThreadsDescription = Resources.OptionMaxNrOfThreadsDescription;

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

        public static string OptionCatchExceptions = Resources.OptionCatchExceptions;
        public const bool OptionCatchExceptionsDefaultValue = true;
        public static string OptionCatchExceptionsDescription =
            string.Format(Resources.OptionCatchExceptionsDescription, GoogleTestConstants.CatchExceptions);

        public virtual bool CatchExceptions => _currentSettings.CatchExceptions ?? OptionCatchExceptionsDefaultValue;


        public static string OptionBreakOnFailure = Resources.OptionBreakOnFailure;
        public const bool OptionBreakOnFailureDefaultValue = false;
        public static string OptionBreakOnFailureDescription =
            string.Format(Resources.OptionBreakOnFailureDescription, GoogleTestConstants.BreakOnFailure);

        public virtual bool BreakOnFailure => _currentSettings.BreakOnFailure ?? OptionBreakOnFailureDefaultValue;


        public static string OptionRunDisabledTests = Resources.OptionRunDisabledTests;
        public const bool OptionRunDisabledTestsDefaultValue = false;
        public static string OptionRunDisabledTestsDescription =
            string.Format(Resources.OptionRunDisabledTestsDescription, GoogleTestConstants.AlsoRunDisabledTestsOption);

        public virtual bool RunDisabledTests => _currentSettings.RunDisabledTests ?? OptionRunDisabledTestsDefaultValue;


        public static string OptionNrOfTestRepetitions = Resources.OptionNrOfTestRepetitions;
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;
        public static string OptionNrOfTestRepetitionsDescription =
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


        public static string OptionShuffleTests = Resources.OptionShuffleTests;
        public const bool OptionShuffleTestsDefaultValue = false;
        public static string OptionShuffleTestsDescription =
            string.Format(Resources.OptionShuffleTestsDescription, GoogleTestConstants.ShuffleTestsOption);

        public virtual bool ShuffleTests => _currentSettings.ShuffleTests ?? OptionShuffleTestsDefaultValue;


        public static string OptionShuffleTestsSeed = Resources.OptionShuffleTestsSeed;
        public const int OptionShuffleTestsSeedDefaultValue = GoogleTestConstants.ShuffleTestsSeedDefaultValue;
        public static string OptionShuffleTestsSeedDescription = string.Format(
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