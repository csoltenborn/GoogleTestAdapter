using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.XPath;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using ILogger = GoogleTestAdapter.Common.ILogger;
using TestWindowLogger = Microsoft.VisualStudio.TestWindow.Extensibility.ILogger;

namespace GoogleTestAdapter.VsPackage.Automation
{
    [ComVisible(true)]
    [Guid("BCC06064-85FD-44EF-97A4-EC3A478FD00F")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IGoogleTestAdapterAutomation))]
    public class GoogleTestAdapterAutomation : IGoogleTestAdapterAutomation
    {
        public const string Name = "GoogleTestAdapter";
        public const int SchemaVersion = 1;

        private readonly Func<RunSettingsService> _runSettingsService;
        private readonly string _name;
        private readonly Version _version;
        private readonly TestDiscoveryJobs _testDiscoveryJobs = new TestDiscoveryJobs();

        /// <param name="runSettingsService">null while the package is not initialized yet</param>
        public GoogleTestAdapterAutomation(Func<RunSettingsService> runSettingsService, string name, Version version)
        {
            _runSettingsService = runSettingsService;
            _name = name;
            _version = version;
        }

        public string GetVersion()
        {
            return Execute(result =>
            {
                result.Add("name", _name);
                result.Add("version", _version.ToString());
            });
        }

        public string GetRunSettings(string runSettingsFile)
        {
            return Execute((result, logger) =>
            {
                result.Add("runSettings", CreateRunSettings(runSettingsFile, logger));
            });
        }

        public string GetEffectiveSettings(string executable)
        {
            return Execute((result, logger) =>
            {
                executable = Path.GetFullPath(executable);
                result.Add("executable", executable);
                ExecuteWithSettings(executable, logger, (settings, container) =>
                {
                    result.Add("projectSettings", ToJson(container.GetSettingsForExecutable(executable)));
                    result.Add("settings", GetSettingValues(settings));
                    result.Add("discovery", new JsonObject
                    {
                        { "workingDir", settings.GetWorkingDirForDiscovery(executable) },
                        { "pathExtension", settings.GetPathExtension(executable) },
                        { "environmentVariables", settings.GetEnvironmentVariablesForDiscovery(executable) },
                        { "additionalParameters", settings.GetUserParametersForDiscovery(executable) }
                    });
                });
            });
        }

        public string CheckExecutable(string executable)
        {
            return Execute((result, logger) =>
            {
                executable = Path.GetFullPath(executable);
                ExecuteWithSettings(executable, logger, (settings, container) =>
                {
                    ExecutableInfo info = ExecutableInfo.Create(executable, settings, logger);
                    result.Add("executable", info.Executable);
                    result.Add("exists", info.Exists);
                    result.Add("isExe", info.IsExe);
                    result.Add("trust", new JsonObject
                    {
                        { "securityZone", info.SecurityZone },
                        { "isOnNetworkDrive", info.IsOnNetworkDrive },
                        { "skipOriginCheck", info.SkipOriginCheck },
                        { "isTrusted", info.IsTrusted }
                    });
                    result.Add("googleTestIndicator", info.GoogleTestIndicator);
                    result.Add("testDiscoveryRegex", info.TestDiscoveryRegex);
                    result.Add("pdb", info.Pdb);
                    result.Add("wouldBeDiscovered", info.WouldBeDiscovered);
                });
            });
        }

        public string StartTestDiscovery(string executable)
        {
            return Execute((result, logger) =>
            {
                executable = Path.GetFullPath(executable);
                // the run settings are created on the calling thread, since they depend on the solution's state
                string runSettings = CreateRunSettings(null, logger);
                TestDiscoveryJobs.Job job = _testDiscoveryJobs.Start(executable, j => DiscoverTests(j, runSettings));
                result.Add("discoveryId", job.Id);
                result.Add("executable", executable);
            });
        }

        public string GetTestDiscoveryResult(string discoveryId)
        {
            return Execute(result =>
            {
                result.Add("discoveryId", discoveryId);
                TestDiscoveryJobs.Job job = _testDiscoveryJobs.Get(discoveryId);
                if (job == null)
                {
                    result.Add("status", TestDiscoveryJobs.Unknown);
                    return;
                }

                string status = job.Status;
                result.Add("status", status);
                result.Add("executable", job.Executable);
                result.Add("durationInSeconds", Math.Round(job.Stopwatch.Elapsed.TotalSeconds, 3));
                if (status == TestDiscoveryJobs.Completed)
                    result.Add("tests", job.Tests);
                result.Add("discoveryMessages", job.Logger.Messages);
            });
        }

        // runs on a background thread
        private bool DiscoverTests(TestDiscoveryJobs.Job job, string runSettings)
        {
            ILogger logger = job.Logger;
            try
            {
                bool succeeded = false;
                ExecuteWithSettings(job.Executable, runSettings, logger, (settings, container) =>
                {
                    // the executable is only run if GTA's discovery would run it
                    ExecutableInfo info = ExecutableInfo.Create(job.Executable, settings, logger);
                    if (!info.WouldBeDiscovered)
                    {
                        logger.LogError($"{_name} would not discover the tests of '{job.Executable}' (see CheckExecutable()): " +
                                        $"exists: {info.Exists}, is .exe: {info.IsExe}, trusted: {info.IsTrusted}, Google Test executable: {info.GoogleTestIndicator != null}");
                        return;
                    }

                    job.Tests = GoogleTestDiscoverer.GetTestsFromExecutable(job.Executable, settings, logger)
                        .Select(testCase => ToJson(testCase, settings.GetTestPropertySettings(job.Executable, testCase.FullyQualifiedName)))
                        .ToList();
                    // e.g. if the executable can not be run, GTA reports an error and no tests
                    succeeded = !job.Logger.HasErrors;
                });
                return succeeded;
            }
            catch (Exception e)
            {
                logger.LogError($"{e.GetType().Name}: {e.Message}");
                return false;
            }
        }

        private static JsonObject ToJson(TestCase testCase, TestPropertySettings testProperties)
        {
            return new JsonObject
            {
                { "fullyQualifiedName", testCase.FullyQualifiedName },
                { "displayName", testCase.DisplayName },
                { "kind", testCase.IsExitCodeTestCase ? "ExitCode" : testCase.IsTearDownTestSuiteTestCase ? "TearDownTestSuite" : "Test" },
                { "codeFilePath", string.IsNullOrEmpty(testCase.CodeFilePath) ? null : testCase.CodeFilePath },
                { "lineNumber", testCase.LineNumber },
                { "namespace", testCase.Namespace },
                { "testClass", testCase.TestClass },
                { "testGroup", testCase.TestGroup },
                { "traits", testCase.Traits.Select(t => new JsonObject { { "name", t.Name }, { "value", t.Value } }).ToList() },
                { "testProperties", testProperties == null ? null : new JsonObject
                    {
                        { "workingDirectory", testProperties.WorkingDirectory },
                        { "environment", testProperties.Environment },
                        { "labels", testProperties.Labels },
                        { "timeoutInSeconds", testProperties.Timeout?.TotalSeconds },
                        { "disabled", testProperties.Disabled }
                    }
                }
            };
        }

        private string CreateRunSettings(string runSettingsFile, ILogger logger)
        {
            RunSettingsService runSettingsService = _runSettingsService();
            if (runSettingsService == null)
                throw new InvalidOperationException($"{_name} has not been initialized yet");

            var document = new XmlDocument { XmlResolver = null };
            if (string.IsNullOrEmpty(runSettingsFile))
                document.LoadXml("<RunSettings />");
            else
                document.Load(runSettingsFile);

            XPathNavigator navigator = runSettingsService.AddRunSettings(document, null, new TestWindowLoggerAdapter(logger)).CreateNavigator();
            navigator.MoveToRoot();
            return navigator.OuterXml;
        }

        // the settings for the executable as the test adapter creates them from the run settings
        private void ExecuteWithSettings(string executable, ILogger logger, Action<SettingsWrapper, RunSettingsContainer> action)
        {
            ExecuteWithSettings(executable, CreateRunSettings(null, logger), logger, action);
        }

        private static void ExecuteWithSettings(string executable, string runSettings, ILogger logger, Action<SettingsWrapper, RunSettingsContainer> action)
        {
            XPathNavigator navigator = new XPathDocument(new StringReader(runSettings)).CreateNavigator();
            RunSettingsContainer container = navigator.MoveToChild(Microsoft.VisualStudio.TestPlatform.ObjectModel.Constants.RunSettingsName, "")
                                             && navigator.MoveToChild(GoogleTestConstants.SettingsName, "")
                ? RunSettingsContainer.LoadFromXml(navigator)
                : new RunSettingsContainer();

            SettingsWrapper settings = CommonFunctions.CreateSettings(container);
            CommonFunctions.InitializeSettings(settings, runSettings, logger);
            settings.ExecuteWithSettingsForExecutable(executable, logger, () => action(settings, container));
        }

        private static object ToJson(RunSettings projectSettings)
        {
            return projectSettings == null ? null : new JsonObject { { "projectRegex", projectSettings.ProjectRegex } };
        }

        private static JsonObject GetSettingValues(SettingsWrapper settings)
        {
            var values = new JsonObject();
            foreach (var setting in new SettingsPrinter(settings).GetValues().Where(s => IsSettingValue(s.Value)))
            {
                values.Add(setting.Key, setting.Value is IEnumerable<RegexTraitPair> pairs
                    ? pairs.Select(p => p.ToString()).ToList()
                    : setting.Value);
            }
            return values;
        }

        // SettingsWrapper also has properties providing helpers (e.g. parsers), which are not settings
        private static bool IsSettingValue(object value)
        {
            return value == null || value is string || value is bool || value is Enum || value.GetType().IsPrimitive
                   || value is IEnumerable<RegexTraitPair>;
        }

        private string Execute(Action<JsonObject> action)
        {
            return Execute((result, logger) => action(result));
        }

        private string Execute(Action<JsonObject, ILogger> action)
        {
            var result = new JsonObject { { "schemaVersion", SchemaVersion } };
            var logger = new CollectingLogger();
            try
            {
                action(result, logger);
            }
            catch (Exception e)
            {
                logger.LogError($"{e.GetType().Name}: {e.Message}");
            }
            result.Add("messages", logger.Messages);
            return Json.Write(result);
        }

        private class TestWindowLoggerAdapter : TestWindowLogger
        {
            private readonly ILogger _logger;

            public TestWindowLoggerAdapter(ILogger logger)
            {
                _logger = logger;
            }

            public void Clear()
            {
            }

            public void Log(MessageLevel messageLevel, string message)
            {
                switch (messageLevel)
                {
                    case MessageLevel.Error:
                        _logger.LogError(message);
                        break;
                    case MessageLevel.Warning:
                        _logger.LogWarning(message);
                        break;
                    default:
                        _logger.LogInfo(message);
                        break;
                }
            }
        }
    }
}
