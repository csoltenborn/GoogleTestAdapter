using System;
using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.VsPackage.GTA.Helpers;
using GoogleTestAdapter.VsPackage.OptionsPages;
using GoogleTestAdapter.VsPackage.ReleaseNotes;
using Microsoft.Win32;

namespace VsPackage.Shared.Settings
{
    public class OptionsUpdater
    {
        private static readonly string OptionsBase = $@"SOFTWARE\Microsoft\VisualStudio\{VsVersionUtils.VsVersion.VersionString()}\DialogPage\GoogleTestAdapter.VsPackage.OptionsPages.";

        private static readonly string GeneralOptionsPage = OptionsBase + "GeneralOptionsDialogPage";
        private static readonly string ParallelizationOptionsPage = OptionsBase + "ParallelizationOptionsDialogPage";

        private const string SettingsVersion = "SettingsVersion";

        private readonly TestDiscoveryOptionsDialogPage _testDiscoveryOptions;
        private readonly TestExecutionOptionsDialogPage _testExecutionOptions;
        private readonly GeneralOptionsDialogPage _generalOptions;
        private readonly ILogger _logger;

        public OptionsUpdater(
            TestDiscoveryOptionsDialogPage testDiscoveryOptions, 
            TestExecutionOptionsDialogPage testExecutionOptions, 
            GeneralOptionsDialogPage generalOptions, 
            ILogger logger)
        {
            _testDiscoveryOptions = testDiscoveryOptions;
            _testExecutionOptions = testExecutionOptions;
            _generalOptions = generalOptions;
            _logger = logger;
        }

        public bool UpdateIfNecessary()
        {
            try
            {
                return TryUpdateIfNecessary();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while updating options: {e}");
                return false;
            }
        }

        private bool TryUpdateIfNecessary()
        {
            if (VsSettingsStorage.Instance.PropertyExists(SettingsVersion))
                return false;

            string versionString = History.Versions.Last().ToString();
            try
            {
                VsSettingsStorage.Instance.SetString(SettingsVersion, versionString);
                UpdateSettings();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception caught while saving SettingsVersion. versionString: {versionString}. Exception:{Environment.NewLine}{e}");
                return false;
            }
        }

        private void UpdateSettings()
        {
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestDiscoveryOptionsDialogPage.TestDiscoveryTimeoutInSeconds), int.Parse, out var testDiscoveryTimeoutInSeconds)) { _testDiscoveryOptions.TestDiscoveryTimeoutInSeconds = testDiscoveryTimeoutInSeconds; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestDiscoveryOptionsDialogPage.TestDiscoveryRegex), s => s, out var testDiscoveryRegex)) { _testDiscoveryOptions.TestDiscoveryRegex = testDiscoveryRegex; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestDiscoveryOptionsDialogPage.ParseSymbolInformation), bool.Parse, out var parseSymbolInformation)) { _testDiscoveryOptions.ParseSymbolInformation = parseSymbolInformation; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestDiscoveryOptionsDialogPage.TestNameSeparator), s => s, out var testNameSeparator)) { _testDiscoveryOptions.TestNameSeparator = testNameSeparator; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestDiscoveryOptionsDialogPage.TraitsRegexesBefore), s => s, out var traitsRegexesBefore)) { _testDiscoveryOptions.TraitsRegexesBefore = traitsRegexesBefore; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestDiscoveryOptionsDialogPage.TraitsRegexesAfter), s => s, out var traitsRegexesAfter)) { _testDiscoveryOptions.TraitsRegexesAfter = traitsRegexesAfter; }

            if (GetAndDeleteValue(ParallelizationOptionsPage, nameof(TestExecutionOptionsDialogPage.EnableParallelTestExecution), bool.Parse, out var enableParallelTestExecution)) { _testExecutionOptions.EnableParallelTestExecution = enableParallelTestExecution; }
            if (GetAndDeleteValue(ParallelizationOptionsPage, nameof(TestExecutionOptionsDialogPage.MaxNrOfThreads), int.Parse, out var maxNrOfThreads)) { _testExecutionOptions.MaxNrOfThreads = maxNrOfThreads; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.AdditionalPdbs), s => s, out var additionalPdbs)) { _testExecutionOptions.AdditionalPdbs = additionalPdbs; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.AdditionalTestExecutionParams), s => s, out var additionalTestExecutionParams)) { _testExecutionOptions.AdditionalTestExecutionParams = additionalTestExecutionParams; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.BatchForTestSetup), s => s, out var batchForTestSetup)) { _testExecutionOptions.BatchForTestSetup = batchForTestSetup; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.BatchForTestTeardown), s => s, out var batchForTestTeardown)) { _testExecutionOptions.BatchForTestTeardown = batchForTestTeardown; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.ExitCodeTestCase), s => s, out var exitCodeTestCase)) { _testExecutionOptions.ExitCodeTestCase = exitCodeTestCase; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.PathExtension), s => s, out var pathExtension)) { _testExecutionOptions.PathExtension = pathExtension; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.KillProcessesOnCancel), bool.Parse, out var killProcessesOnCancel)) { _testExecutionOptions.KillProcessesOnCancel = killProcessesOnCancel; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(TestExecutionOptionsDialogPage.WorkingDir), s => s, out var workingDir)) { _testExecutionOptions.WorkingDir = workingDir; }
            
            if (GetAndDeleteValue(GeneralOptionsPage, "UseNewTestExecutionFramework2", bool.Parse, out var useNewTestExecutionFramework2)) { _testExecutionOptions.DebuggerKind = useNewTestExecutionFramework2 ? DebuggerKind.Native : DebuggerKind.VsTestFramework; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(IGoogleTestAdapterSettings.DebugMode), bool.Parse, out var debugMode)) { _generalOptions.OutputMode = debugMode ? OutputMode.Debug : OutputMode.Info; }
            if (GetAndDeleteValue(GeneralOptionsPage, nameof(IGoogleTestAdapterSettings.TimestampOutput), bool.Parse, out bool timestampOutput)) { _generalOptions.TimestampMode = GetTimestampMode(timestampOutput); }
            GetAndDeleteValue(GeneralOptionsPage, nameof(IGoogleTestAdapterSettings.ShowReleaseNotes), bool.Parse, out _);

            DeleteSubkey(ParallelizationOptionsPage);
        }

        private static bool GetAndDeleteValue<T>(string optionsKey, string propertyName, Func<string, T> map, out T value)
        {
            try
            {
                using (var registryKey = Registry.CurrentUser.OpenSubKey(optionsKey, true))
                {
                    string valueString = registryKey?.GetValue(propertyName)?.ToString();
                    if (valueString != null)
                    {
                        value = map(valueString);
                        registryKey.DeleteValue(propertyName);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // too bad
            }

            value = default(T);
            return false;
        }

        private static void DeleteSubkey(string subkey)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKey(subkey);
            }
            catch (Exception)
            {
                // too bad
            }
        }

        private TimestampMode GetTimestampMode(bool timestampOutput)
        {
            if (timestampOutput)
            {
                return VsVersionUtils.VsVersion < VsVersion.VS2017
                    ? TimestampMode.Automatic
                    : TimestampMode.PrintTimestamp;
            }
            return VsVersionUtils.VsVersion < VsVersion.VS2017
                ? TimestampMode.DoNotPrintTimestamp
                : TimestampMode.Automatic;
        }

    }
}