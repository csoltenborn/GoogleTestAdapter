using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;

namespace GoogleTestAdapter.Settings
{
    public class PlaceholderReplacer
    {
        private const string OnlyInsideVs = " (only available inside VS)";
        private const string TestExecutionOnly = " (test execution only)";

        public const string SolutionDirPlaceholder = "$(SolutionDir)";
        private const string DescriptionOfSolutionDirPlaceHolder = SolutionDirPlaceholder + " - directory of the solution" + OnlyInsideVs;

        public const string PlatformNamePlaceholder = "$(PlatformName)";
        private const string DescriptionOfPlatformNamePlaceholder = PlatformNamePlaceholder + " - the name of the solution's current platform" + OnlyInsideVs;

        public const string ConfigurationNamePlaceholder = "$(ConfigurationName)";
        private const string DescriptionOfConfigurationNamePlaceholder = ConfigurationNamePlaceholder + " - the name of the solution's current configuration" + OnlyInsideVs;

        public const string ExecutablePlaceholder = "$(Executable)";
        private const string DescriptionOfExecutablePlaceHolder = ExecutablePlaceholder + " - executable containing the tests";

        public const string ExecutableDirPlaceholder = "$(ExecutableDir)";
        public const string DescriptionOfExecutableDirPlaceHolder = ExecutableDirPlaceholder + " - directory containing the test executable";

        public const string TestDirPlaceholder = "$(TestDir)";
        private const string DescriptionOfTestDirPlaceholder = TestDirPlaceholder + " - path of a directory which can be used by the tests";

        public const string ThreadIdPlaceholder = "$(ThreadId)";
        private const string DescriptionOfThreadIdPlaceholder = ThreadIdPlaceholder + " - id of thread executing the current tests";

        private const string DescriptionOfEnvVarPlaceholders = "Environment variables are also possible, e.g. %PATH%";


        private static readonly Regex PlaceholdersRegex = new Regex(@"\$\(\w+\)", RegexOptions.Compiled);

        private readonly Func<string> _getSolutionDir;
        private readonly Func<IGoogleTestAdapterSettings> _getSettings;
        private readonly HelperFilesCache _helperFilesCache;
        private readonly ILogger _logger;

        private string SolutionDir => _getSolutionDir();
        private IGoogleTestAdapterSettings Settings => _getSettings();

        public PlaceholderReplacer(Func<string> getSolutionDir, Func<IGoogleTestAdapterSettings> getSettings, HelperFilesCache helperFilesCache, ILogger logger)
        {
            _getSolutionDir = getSolutionDir;
            _getSettings = getSettings;
            _helperFilesCache = helperFilesCache;
            _logger = logger;
        }


        public const string AdditionalPdbsPlaceholders = "Placeholders:\n" +
                                                         DescriptionOfSolutionDirPlaceHolder + "\n" +
                                                         DescriptionOfPlatformNamePlaceholder + "\n" +
                                                         DescriptionOfConfigurationNamePlaceholder + "\n" +
                                                         DescriptionOfExecutableDirPlaceHolder + "\n" +
                                                         DescriptionOfExecutablePlaceHolder + "\n" + 
                                                         DescriptionOfEnvVarPlaceholders;

        public string ReplaceAdditionalPdbsPlaceholders(string pdb, string executable)
        {
            pdb = ReplaceExecutablePlaceholders(pdb.Trim(), executable);
            pdb = ReplacePlatformAndConfigurationPlaceholders(pdb, executable);
            pdb = ReplaceSolutionDirPlaceholder(pdb, executable);
            pdb = ReplaceEnvironmentVariables(pdb);
            pdb = ReplaceHelperFileSettings(pdb, executable);

            CheckForRemainingPlaceholders(pdb, SettingsWrapper.OptionAdditionalPdbs);
            
            return pdb;
        }


        public const string WorkingDirPlaceholders = "Placeholders:\n" + 
                                                     DescriptionOfSolutionDirPlaceHolder + "\n" + 
                                                     DescriptionOfPlatformNamePlaceholder + "\n" + 
                                                     DescriptionOfConfigurationNamePlaceholder + "\n" + 
                                                     DescriptionOfExecutableDirPlaceHolder + "\n" + 
                                                     DescriptionOfExecutablePlaceHolder + "\n" + 
                                                     DescriptionOfTestDirPlaceholder + TestExecutionOnly + "\n" + 
                                                     DescriptionOfThreadIdPlaceholder + TestExecutionOnly + "\n" + 
                                                     DescriptionOfEnvVarPlaceholders;

        public string ReplaceWorkingDirPlaceholdersForDiscovery(string workingDir, string executable)
        {
            workingDir = ReplaceExecutablePlaceholders(workingDir, executable);
            workingDir = RemoveTestDirAndThreadIdPlaceholders(workingDir);
            workingDir = ReplacePlatformAndConfigurationPlaceholders(workingDir, executable);
            workingDir = ReplaceSolutionDirPlaceholder(workingDir, executable);
            workingDir = ReplaceEnvironmentVariables(workingDir);
            workingDir = ReplaceHelperFileSettings(workingDir, executable);

            CheckForRemainingPlaceholders(workingDir, SettingsWrapper.OptionWorkingDir);

            return workingDir;
        }

        public string ReplaceWorkingDirPlaceholdersForExecution(string workingDir, string executable,
            string testDirectory, int threadId)
        {
            workingDir = ReplaceTestDirAndThreadIdPlaceholders(workingDir, testDirectory, threadId);
            workingDir = ReplaceExecutablePlaceholders(workingDir, executable);
            workingDir = ReplacePlatformAndConfigurationPlaceholders(workingDir, executable);
            workingDir = ReplaceSolutionDirPlaceholder(workingDir, executable);
            workingDir = ReplaceEnvironmentVariables(workingDir);
            workingDir = ReplaceHelperFileSettings(workingDir, executable);

            CheckForRemainingPlaceholders(workingDir, SettingsWrapper.OptionWorkingDir);

            return workingDir;
        }


        public const string PathExtensionPlaceholders = "Placeholders:\n" +
                                                        DescriptionOfSolutionDirPlaceHolder + "\n" +
                                                        DescriptionOfPlatformNamePlaceholder + "\n" +
                                                        DescriptionOfConfigurationNamePlaceholder + "\n" +
                                                        DescriptionOfExecutableDirPlaceHolder + "\n" +
                                                        DescriptionOfExecutablePlaceHolder + "\n" + 
                                                        DescriptionOfEnvVarPlaceholders;

        public string ReplacePathExtensionPlaceholders(string pathExtension, string executable)
        {
            pathExtension = ReplaceExecutablePlaceholders(pathExtension, executable);
            pathExtension = ReplacePlatformAndConfigurationPlaceholders(pathExtension, executable);
            pathExtension = ReplaceSolutionDirPlaceholder(pathExtension, executable);
            pathExtension = ReplaceEnvironmentVariables(pathExtension);
            pathExtension = ReplaceHelperFileSettings(pathExtension, executable);

            CheckForRemainingPlaceholders(pathExtension, SettingsWrapper.OptionPathExtension);

            return pathExtension;
        }


        public const string EnvironmentPlaceholders = "Placeholders:\n" +
                                                        DescriptionOfSolutionDirPlaceHolder + "\n" +
                                                        DescriptionOfPlatformNamePlaceholder + "\n" +
                                                        DescriptionOfConfigurationNamePlaceholder + "\n" +
                                                        DescriptionOfExecutableDirPlaceHolder + "\n" +
                                                        DescriptionOfExecutablePlaceHolder + "\n" + 
                                                        DescriptionOfTestDirPlaceholder + TestExecutionOnly + "\n" + 
                                                        DescriptionOfThreadIdPlaceholder + TestExecutionOnly + "\n" + 
                                                        DescriptionOfEnvVarPlaceholders;

        public string ReplaceEnvironmentVariablesPlaceholdersForExecution(string environmentVariables, string executable, string testDirectory, int threadId)
        {
            environmentVariables =
                ReplaceTestDirAndThreadIdPlaceholders(environmentVariables, testDirectory, threadId);
            environmentVariables = ReplaceExecutablePlaceholders(environmentVariables, executable);
            environmentVariables = ReplacePlatformAndConfigurationPlaceholders(environmentVariables, executable);
            environmentVariables = ReplaceSolutionDirPlaceholder(environmentVariables, executable);
            environmentVariables = ReplaceEnvironmentVariables(environmentVariables);
            environmentVariables = ReplaceHelperFileSettings(environmentVariables, executable);

            CheckForRemainingPlaceholders(environmentVariables, SettingsWrapper.OptionAdditionalTestExecutionParams);

            return environmentVariables;
        }

        public string ReplaceEnvironmentVariablesPlaceholdersForDiscovery(string environmentVariables, string executable)
        {
            environmentVariables = ReplaceExecutablePlaceholders(environmentVariables, executable);
            environmentVariables = RemoveTestDirAndThreadIdPlaceholders(environmentVariables);
            environmentVariables = ReplacePlatformAndConfigurationPlaceholders(environmentVariables, executable);
            environmentVariables = ReplaceSolutionDirPlaceholder(environmentVariables, executable);
            environmentVariables = ReplaceEnvironmentVariables(environmentVariables);
            environmentVariables = ReplaceHelperFileSettings(environmentVariables, executable);

            CheckForRemainingPlaceholders(environmentVariables, SettingsWrapper.OptionAdditionalTestExecutionParams);

            return environmentVariables;
        }


        public const string AdditionalTestExecutionParamPlaceholders = "Placeholders:\n" + 
                                                                       DescriptionOfSolutionDirPlaceHolder + "\n" + 
                                                                       DescriptionOfPlatformNamePlaceholder + "\n" + 
                                                                       DescriptionOfConfigurationNamePlaceholder + "\n" + 
                                                                       DescriptionOfExecutableDirPlaceHolder + "\n" + 
                                                                       DescriptionOfExecutablePlaceHolder + "\n" + 
                                                                       DescriptionOfTestDirPlaceholder + TestExecutionOnly + "\n" + 
                                                                       DescriptionOfThreadIdPlaceholder + TestExecutionOnly + "\n" + 
                                                                       DescriptionOfEnvVarPlaceholders;

        public string ReplaceAdditionalTestExecutionParamPlaceholdersForDiscovery(string additionalTestExecutionParam, string executable)
        {
            additionalTestExecutionParam = ReplaceExecutablePlaceholders(additionalTestExecutionParam, executable);
            additionalTestExecutionParam = RemoveTestDirAndThreadIdPlaceholders(additionalTestExecutionParam);
            additionalTestExecutionParam = ReplacePlatformAndConfigurationPlaceholders(additionalTestExecutionParam, executable);
            additionalTestExecutionParam = ReplaceSolutionDirPlaceholder(additionalTestExecutionParam, executable);
            additionalTestExecutionParam = ReplaceEnvironmentVariables(additionalTestExecutionParam);
            additionalTestExecutionParam = ReplaceHelperFileSettings(additionalTestExecutionParam, executable);

            CheckForRemainingPlaceholders(additionalTestExecutionParam, SettingsWrapper.OptionAdditionalTestExecutionParams);

            return additionalTestExecutionParam;
        }

        public string ReplaceAdditionalTestExecutionParamPlaceholdersForExecution(string additionalTestExecutionParam, string executable, string testDirectory, int threadId)
        {
            additionalTestExecutionParam =
                ReplaceTestDirAndThreadIdPlaceholders(additionalTestExecutionParam, testDirectory, threadId);
            additionalTestExecutionParam = ReplaceExecutablePlaceholders(additionalTestExecutionParam, executable);
            additionalTestExecutionParam = ReplacePlatformAndConfigurationPlaceholders(additionalTestExecutionParam, executable);
            additionalTestExecutionParam = ReplaceSolutionDirPlaceholder(additionalTestExecutionParam, executable);
            additionalTestExecutionParam = ReplaceEnvironmentVariables(additionalTestExecutionParam);
            additionalTestExecutionParam = ReplaceHelperFileSettings(additionalTestExecutionParam, executable);

            CheckForRemainingPlaceholders(additionalTestExecutionParam, SettingsWrapper.OptionAdditionalTestExecutionParams);

            return additionalTestExecutionParam;
        }


        public const string BatchesPlaceholders = "Placeholders:\n" + 
                                                  DescriptionOfSolutionDirPlaceHolder + "\n" + 
                                                  DescriptionOfPlatformNamePlaceholder + "\n" + 
                                                  DescriptionOfConfigurationNamePlaceholder + "\n" + 
                                                  DescriptionOfTestDirPlaceholder + "\n" + 
                                                  DescriptionOfThreadIdPlaceholder + "\n" + 
                                                  DescriptionOfEnvVarPlaceholders;


        public string ReplaceSetupBatchPlaceholders(string batch, string testDirectory, int threadId)
        {
            batch = ReplaceBatchPlaceholders(batch, testDirectory, threadId);
            CheckForRemainingPlaceholders(batch, SettingsWrapper.OptionBatchForTestSetup);
            return batch;
        }

        public string ReplaceTeardownBatchPlaceholders(string batch, string testDirectory, int threadId)
        {
            batch = ReplaceBatchPlaceholders(batch, testDirectory, threadId);
            CheckForRemainingPlaceholders(batch, SettingsWrapper.OptionBatchForTestTeardown);
            return batch;
        }

        private string ReplaceBatchPlaceholders(string batch, string testDirectory, int threadId)
        {
            batch = ReplaceTestDirAndThreadIdPlaceholders(batch, testDirectory, threadId);
            batch = ReplacePlatformAndConfigurationPlaceholders(batch);
            batch = ReplaceSolutionDirPlaceholder(batch);
            batch = ReplaceEnvironmentVariables(batch);
            return batch;
        }


        private string ReplaceSolutionDirPlaceholder(string theString, string executable = null)
        {
            return string.IsNullOrWhiteSpace(theString)
                ? ""
                : ReplaceValueWithHelperFile(theString, SolutionDirPlaceholder, SolutionDir,
                    executable, nameof(IGoogleTestAdapterSettings.SolutionDir));
        }

        private string ReplacePlatformAndConfigurationPlaceholders(string theString, string executable = null)
        {
            if (string.IsNullOrWhiteSpace(theString))
                return "";

            theString = ReplaceValueWithHelperFile(theString, PlatformNamePlaceholder, Settings.PlatformName, 
                executable, nameof(IGoogleTestAdapterSettings.PlatformName));
            theString = ReplaceValueWithHelperFile(theString, ConfigurationNamePlaceholder, Settings.ConfigurationName, 
                executable, nameof(IGoogleTestAdapterSettings.ConfigurationName));
            return theString;
        }

        private string ReplaceExecutablePlaceholders(string theString, string executable)
        {
            return string.IsNullOrWhiteSpace(theString)
                ? ""
                : theString
                    // ReSharper disable once PossibleNullReferenceException
                    .Replace(ExecutableDirPlaceholder, new FileInfo(executable).Directory.FullName)
                    .Replace(ExecutablePlaceholder, executable);
        }

        private string ReplaceTestDirAndThreadIdPlaceholders(string theString, string testDirectory, int threadId)
        {
            return ReplaceTestDirAndThreadIdPlaceholders(theString, testDirectory, threadId.ToString());
        }

        private string RemoveTestDirAndThreadIdPlaceholders(string theString)
        {
            return ReplaceTestDirAndThreadIdPlaceholders(theString, "", "");
        }

        private string ReplaceEnvironmentVariables(string theString)
        {
            return string.IsNullOrWhiteSpace(theString) 
                ? "" 
                : Environment.ExpandEnvironmentVariables(theString);
        }

        private string ReplaceTestDirAndThreadIdPlaceholders(string theString, string testDirectory, string threadId)
        {
            return string.IsNullOrWhiteSpace(theString)
                ? ""
                : theString
                    .Replace(TestDirPlaceholder, testDirectory)
                    .Replace(ThreadIdPlaceholder, threadId);
        }

        private string ReplaceHelperFileSettings(string theString, string executable)
        {
            var replacementMap = _helperFilesCache.GetReplacementsMap(executable);
            foreach (var nameValuePair in replacementMap)
            {
                theString = theString.Replace($"$({nameValuePair.Key})", nameValuePair.Value);
            }

            return theString;
        }

        private string ReplaceValueWithHelperFile(string theString, string placeholder, string value, string executable,
            string settingName)
        {
            if (string.IsNullOrWhiteSpace(value) && executable != null)
            {
                var map = _helperFilesCache.GetReplacementsMap(executable);
                if (map.TryGetValue(settingName, out string helperFileValue))
                {
                    value = helperFileValue;
                }
            }

            return string.IsNullOrWhiteSpace(value)
                ? theString.Replace(placeholder, "")
                : theString.Replace(placeholder, value);
        }

        private void CheckForRemainingPlaceholders(string theString, string optionName)
        {
            var matches = PlaceholdersRegex.Matches(theString);
            if (matches.Count > 0)
            {

                var placeholders = matches.Cast<Match>().Select(m => m.Value).Distinct().OrderBy(s => s);
                string message = $"Option '{optionName}': Apparently, the following placeholders could not be replaced. {string.Join(", ", placeholders)}";
                _logger.LogWarning(message);
            }
        }

    }

}