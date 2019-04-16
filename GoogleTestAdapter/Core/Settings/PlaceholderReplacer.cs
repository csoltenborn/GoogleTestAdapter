using System;
using System.IO;

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


        private readonly Func<string> _getSolutionDir;
        private readonly Func<IGoogleTestAdapterSettings> _getSettings;

        private string SolutionDir => _getSolutionDir();
        private IGoogleTestAdapterSettings Settings => _getSettings();

        public PlaceholderReplacer(Func<string> getSolutionDir, Func<IGoogleTestAdapterSettings> getSettings)
        {
            _getSolutionDir = getSolutionDir;
            _getSettings = getSettings;
        }


        public const string AdditionalPdbsPlaceholders = "Placeholders:\n" +
                                                         DescriptionOfSolutionDirPlaceHolder + "\n" +
                                                         DescriptionOfPlatformNamePlaceholder + "\n" +
                                                         DescriptionOfConfigurationNamePlaceholder + "\n" +
                                                         DescriptionOfExecutableDirPlaceHolder + "\n" +
                                                         DescriptionOfExecutablePlaceHolder + "\n" + 
                                                         DescriptionOfEnvVarPlaceholders;

        public string ReplaceAdditionalPdbsPlaceholders(string executable, string pdb)
        {
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        ReplaceExecutablePlaceholders(pdb.Trim(), executable))));
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

        public string ReplaceWorkingDirPlaceholdersForDiscovery(string executable, string workingDir)
        {
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        RemoveTestDirAndThreadIdPlaceholders(
                            ReplaceExecutablePlaceholders(workingDir, executable)))));
        }

        public string ReplaceWorkingDirPlaceholdersForExecution(string executable, string workingDir, string testDirectory, int threadId)
        {
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        ReplaceExecutablePlaceholders(
                            ReplaceTestDirAndThreadIdPlaceholders(workingDir, testDirectory, threadId), executable))));
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
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        ReplaceExecutablePlaceholders(pathExtension, executable))));
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
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        RemoveTestDirAndThreadIdPlaceholders(
                            ReplaceExecutablePlaceholders(additionalTestExecutionParam, executable)))));
        }

        public string ReplaceAdditionalTestExecutionParamPlaceholdersForExecution(string additionalTestExecutionParam, string executable, string testDirectory, int threadId)
        {
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        ReplaceExecutablePlaceholders(
                            ReplaceTestDirAndThreadIdPlaceholders(additionalTestExecutionParam, testDirectory, threadId), executable))));
        }



        public const string BatchesPlaceholders = "Placeholders:\n" + 
                                                  DescriptionOfSolutionDirPlaceHolder + "\n" + 
                                                  DescriptionOfPlatformNamePlaceholder + "\n" + 
                                                  DescriptionOfConfigurationNamePlaceholder + "\n" + 
                                                  DescriptionOfTestDirPlaceholder + "\n" + 
                                                  DescriptionOfThreadIdPlaceholder + "\n" + 
                                                  DescriptionOfEnvVarPlaceholders;

        public string ReplaceBatchForTestSetupPlaceholders(string batchForTestSetup, string testDirectory, int threadId)
        {
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        ReplaceTestDirAndThreadIdPlaceholders(batchForTestSetup, testDirectory, threadId))));
        }

        public string ReplaceBatchForTestTeardownPlaceholders(string batchForTestTeardown, string testDirectory, int threadId)
        {
            return ReplaceEnvironmentVariables(
                ReplaceSolutionDirPlaceholder(
                    ReplacePlatformAndConfigurationPlaceholders(
                        ReplaceTestDirAndThreadIdPlaceholders(batchForTestTeardown, testDirectory, threadId))));
        }


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

        private string ReplacePlatformAndConfigurationPlaceholders(string theString)
        {
            if (string.IsNullOrWhiteSpace(theString))
            {
                return "";
            }

            string result = theString;

            result = string.IsNullOrWhiteSpace(Settings.PlatformName) 
                ? result.Replace(PlatformNamePlaceholder, "")
                : result.Replace(PlatformNamePlaceholder, Settings.PlatformName);
            result = string.IsNullOrWhiteSpace(Settings.ConfigurationName) 
                ? result.Replace(ConfigurationNamePlaceholder, "")
                : result.Replace(ConfigurationNamePlaceholder, Settings.ConfigurationName);

            return result;
        }

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
            if (string.IsNullOrWhiteSpace(theString))
            {
                return "";
            }

            return Environment.ExpandEnvironmentVariables(theString);
        }

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

    }

}