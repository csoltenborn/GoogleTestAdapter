// This file has been modified by Microsoft on 8/2017.

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


        public const string CategoryTestExecutionName = "Test execution";
        public const string CategoryTraitsName = "Regexes for trait assignment";
        public const string CategoryRuntimeBehaviorName = "Runtime behavior";
        public const string CategoryParallelizationName = "Parallelization";
        public const string CategoryMiscName = "Misc";

        public const string PageGeneralName = "General";
        public const string PageParallelizationName = CategoryParallelizationName;
        public const string PageGoogleTestName = "Google Test";

        public const string SolutionDirPlaceholder = "$(SolutionDir)";
        public const string TestDirPlaceholder = "$(TestDir)";
        public const string ThreadIdPlaceholder = "$(ThreadId)";
        public const string ExecutablePlaceholder = "$(Executable)";
        public const string ExecutableDirPlaceholder = "$(ExecutableDir)";

        private const string DescriptionOfSolutionDirPlaceHolder =
            SolutionDirPlaceholder + " - directory of the solution (only available inside VS)";

        private const string DescriptionOfExecutableDirPlaceHolder =
            ExecutableDirPlaceholder + " - directory containing the test executable";

        private const string DescriptionOfPlaceholdersForBatches =
            TestDirPlaceholder + " - path of a directory which can be used by the tests\n" +
            ThreadIdPlaceholder + " - id of thread executing the current tests\n" +
            DescriptionOfSolutionDirPlaceHolder;

        private const string DescriptionOfPlaceholdersForExecutables =
            DescriptionOfPlaceholdersForBatches + "\n" +
            ExecutablePlaceholder + " - executable containing the tests\n" +
            DescriptionOfExecutableDirPlaceHolder;

        #region GeneralOptionsPage

        public virtual string DebuggingNamedPipeId => _currentSettings.DebuggingNamedPipeId;

        public const string OptionUseNewTestExecutionFramework = "Use new test execution framework (experimental)";
        public const bool OptionUseNewTestExecutionFrameworkDefaultValue = true;
        public const string OptionUseNewTestExecutionFrameworkDescription =
            "Make use of the new test execution framework. Advantages: test crash detection and test output printing also work in debug mode.";

        public virtual bool UseNewTestExecutionFramework => _currentSettings.UseNewTestExecutionFramework ?? OptionUseNewTestExecutionFrameworkDefaultValue;


        public const string OptionPrintTestOutput = "Print test output";
        public const bool OptionPrintTestOutputDefaultValue = false;
        public const string OptionPrintTestOutputDescription =
            "Print the output of the Google Test executable(s) to the Tests Output window.";

        public virtual bool PrintTestOutput => _currentSettings.PrintTestOutput ?? OptionPrintTestOutputDefaultValue;


        public const string OptionTestDiscoveryRegex = "Regex for test discovery";
        public const string OptionTestDiscoveryRegexDefaultValue = "";
        public const string OptionTestDiscoveryRegexDescription =
            "If non-empty, this regex will be used to discover the Google Test executables containing your tests.\nDefault regex: "
            + TestFinderRegex;

        public virtual string TestDiscoveryRegex => _currentSettings.TestDiscoveryRegex ?? OptionTestDiscoveryRegexDefaultValue;


        public const string OptionTestDiscoveryTimeoutInSeconds = "Test discovery timeout in s";
        public const int OptionTestDiscoveryTimeoutInSecondsDefaultValue = 30;
        public const string OptionTestDiscoveryTimeoutInSecondsDescription =
            "Number of seconds after which test discovery will be assumed to have failed. 0: Infinite timeout";

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
            "If non-empty, will set the working directory for running the tests (default: " + DescriptionOfExecutableDirPlaceHolder + ").\nExample: " + SolutionDirPlaceholder + "\\MyTestDir\nPlaceholders:\n"
            + DescriptionOfExecutableDirPlaceHolder + "\n" + DescriptionOfSolutionDirPlaceHolder;

        public virtual string WorkingDir => _currentSettings.WorkingDir ?? OptionWorkingDirDefaultValue;


        public const string OptionPathExtension = "PATH extension";
        public const string OptionPathExtensionDefaultValue = "";
        public const string OptionPathExtensionDescription =
            "If non-empty, the content will be appended to the PATH variable of the test execution and discovery processes.\nExample: C:\\MyBins;" + ExecutableDirPlaceholder + "\\MyOtherBins;\nPlaceholders:\n"
            + DescriptionOfExecutableDirPlaceHolder;

        public virtual string PathExtension => _currentSettings.PathExtension ?? OptionPathExtensionDefaultValue;


        public const string TraitsRegexesPairSeparator = "//||//";
        public const string TraitsRegexesRegexSeparator = "///";
        public const string TraitsRegexesTraitSeparator = ",";
        public const string OptionTraitsRegexesDefaultValue = "";
        public const string OptionTraitsDescription = "Allows to override/add traits for testcases matching a regex. Traits are build up in 3 phases: 1st, traits are assigned to tests according to the 'Traits before' option. 2nd, the tests' traits (defined via the macros in GTA_Traits.h) are added to the tests, overriding traits from phase 1 with new values. 3rd, the 'Traits after' option is evaluated, again in an overriding manner.\nSyntax: "
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
        public const string OptionTestNameSeparatorDefaultValue = "";
        public const string OptionTestNameSeparatorDescription =
            "Test names produced by Google Test might contain the character '/', which makes VS cut the name after the '/' if the test explorer window is not wide enough. This option's value, if non-empty, will replace the '/' character to avoid that behavior. Note that '\\', ' ', '|', and '-' produce the same behavior ('.', '_', ':', and '::' are known to work - there might be more). Note also that traits regexes are evaluated against the tests' display names (and must thus be consistent with this option).";

        public virtual string TestNameSeparator => _currentSettings.TestNameSeparator ?? OptionTestNameSeparatorDefaultValue;


        public const string OptionParseSymbolInformation = "Parse symbol information";
        public const bool OptionParseSymbolInformationDefaultValue = true;
        public const string OptionParseSymbolInformationDescription =
            "Parse debug symbol information for test executables to obtain source location information and traits (defined via the macros in GTA_Traits.h).\n" +
            "If this is set to false step 2 of traits discovery will be left out and only traits regexes will be effective.";

        public virtual bool ParseSymbolInformation => _currentSettings.ParseSymbolInformation ?? OptionParseSymbolInformationDefaultValue;

        public const string OptionDebugMode = "Print debug info";
        public const bool OptionDebugModeDefaultValue = false;
        public const string OptionDebugModeDescription =
            "If true, debug output will be printed to the test console.";

        public virtual bool DebugMode => _currentSettings.DebugMode ?? OptionDebugModeDefaultValue;


        public const string OptionTimestampOutput = "Timestamp output";
        public const bool OptionTimestampOutputDefaultValue = false;
        public const string OptionTimestampOutputDescription =
            "If true, a timestamp is added to test and debug output.";

        public virtual bool TimestampOutput => _currentSettings.TimestampOutput ?? OptionTimestampOutputDefaultValue;


        public const string OptionShowReleaseNotes = "Show release notes after update";
        public const bool OptionShowReleaseNotesDefaultValue = true;
        public const string OptionShowReleaseNotesDescription =
            "If true, a dialog with release notes is shown after the extension has been updated.";

        public virtual bool ShowReleaseNotes => _currentSettings.ShowReleaseNotes ?? OptionShowReleaseNotesDefaultValue;


        public const string OptionAdditionalTestExecutionParams = "Additional test execution parameters";
        public const string OptionAdditionalTestExecutionParamsDefaultValue = "";
        public const string OptionAdditionalTestExecutionParamsDescription =
            "Additional parameters for Google Test executable. Placeholders:\n"
            + DescriptionOfPlaceholdersForExecutables;

        public virtual string AdditionalTestExecutionParam => _currentSettings.AdditionalTestExecutionParam ?? OptionAdditionalTestExecutionParamsDefaultValue;


        public const string OptionBatchForTestSetup = "Test setup batch file";
        public const string OptionBatchForTestSetupDefaultValue = "";
        public const string OptionBatchForTestSetupDescription =
            "Batch file to be executed before test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n"
            + DescriptionOfPlaceholdersForBatches;

        public virtual string BatchForTestSetup => _currentSettings.BatchForTestSetup ?? OptionBatchForTestSetupDefaultValue;


        public const string OptionBatchForTestTeardown = "Test teardown batch file";
        public const string OptionBatchForTestTeardownDefaultValue = "";
        public const string OptionBatchForTestTeardownDescription =
            "Batch file to be executed after test execution. If tests are executed in parallel, the batch file will be executed once per thread. Placeholders:\n"
            + DescriptionOfPlaceholdersForBatches;

        public virtual string BatchForTestTeardown => _currentSettings.BatchForTestTeardown ?? OptionBatchForTestTeardownDefaultValue;


        public const string OptionKillProcessesOnCancel = "Kill processes on cancel";
        public const bool OptionKillProcessesOnCancelDefaultValue = false;
        public const string OptionKillProcessesOnCancelDescription =
            "If true, running test executables are actively killed if the test execution is canceled. Note that killing a test process might have all kinds of side effects; in particular, Google Test will not be able to perform any shutdown tasks.";

        public virtual bool KillProcessesOnCancel => _currentSettings.KillProcessesOnCancel ?? OptionKillProcessesOnCancelDefaultValue;

        #endregion

        #region ParallelizationOptionsPage

        public const string OptionEnableParallelTestExecution = "Parallel test execution";
        public const bool OptionEnableParallelTestExecutionDefaultValue = false;
        public const string OptionEnableParallelTestExecutionDescription =
            "Parallel test execution is achieved by means of different threads, each of which is assigned a number of tests to be executed. The threads will then sequentially invoke the necessary executables to produce the according test results.";

        public virtual bool ParallelTestExecution => _currentSettings.ParallelTestExecution ?? OptionEnableParallelTestExecutionDefaultValue;


        public const string OptionMaxNrOfThreads = "Maximum number of threads";
        public const int OptionMaxNrOfThreadsDefaultValue = 0;
        public const string OptionMaxNrOfThreadsDescription =
            "Maximum number of threads to be used for test execution (0: one thread for each processor).";

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

        public const string OptionCatchExceptions = "Catch exceptions";
        public const bool OptionCatchExceptionsDefaultValue = true;
        public const string OptionCatchExceptionsDescription =
            "Google Test catches exceptions by default; the according test fails and test execution continues. Choosing false lets exceptions pass through, allowing the debugger to catch them.\n"
            + "Google Test option:" + GoogleTestConstants.CatchExceptions;

        public virtual bool CatchExceptions => _currentSettings.CatchExceptions ?? OptionCatchExceptionsDefaultValue;


        public const string OptionBreakOnFailure = "Break on failure";
        public const bool OptionBreakOnFailureDefaultValue = false;
        public const string OptionBreakOnFailureDescription =
            "If enabled, a potentially attached debugger will catch assertion failures and automatically drop into interactive mode.\n"
            + "Google Test option:" + GoogleTestConstants.BreakOnFailure;

        public virtual bool BreakOnFailure => _currentSettings.BreakOnFailure ?? OptionBreakOnFailureDefaultValue;


        public const string OptionRunDisabledTests = "Also run disabled tests";
        public const bool OptionRunDisabledTestsDefaultValue = false;
        public const string OptionRunDisabledTestsDescription =
            "If true, all (selected) tests will be run, even if they have been disabled.\n"
            + "Google Test option:" + GoogleTestConstants.AlsoRunDisabledTestsOption;

        public virtual bool RunDisabledTests => _currentSettings.RunDisabledTests ?? OptionRunDisabledTestsDefaultValue;


        public const string OptionNrOfTestRepetitions = "Number of test repetitions";
        public const int OptionNrOfTestRepetitionsDefaultValue = 1;
        public const string OptionNrOfTestRepetitionsDescription =
            "Tests will be run for the selected number of times (-1: infinite).\n"
            + "Google Test option:" + GoogleTestConstants.NrOfRepetitionsOption;

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
        public const bool OptionShuffleTestsDefaultValue = false;
        public const string OptionShuffleTestsDescription =
            "If true, tests will be executed in random order. Note that a true randomized order is only given when executing all tests in non-parallel fashion. Otherwise, the test excutables will most likely be executed more than once - random order is than restricted to the according executions.\n"
            + "Google Test option:" + GoogleTestConstants.ShuffleTestsOption;

        public virtual bool ShuffleTests => _currentSettings.ShuffleTests ?? OptionShuffleTestsDefaultValue;


        public const string OptionShuffleTestsSeed = "Shuffle tests: Seed";
        public const int OptionShuffleTestsSeedDefaultValue = GoogleTestConstants.ShuffleTestsSeedDefaultValue;
        public const string OptionShuffleTestsSeedDescription = "0: Seed is computed from system time, 1<=n<="
                                                           + GoogleTestConstants.ShuffleTestsSeedMaxValueAsString
                                                           + ": The given seed is used. See note of option '"
                                                           + OptionShuffleTests
                                                           + "'.\n"
            + "Google Test option:" + GoogleTestConstants.ShuffleTestsSeedOption;

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