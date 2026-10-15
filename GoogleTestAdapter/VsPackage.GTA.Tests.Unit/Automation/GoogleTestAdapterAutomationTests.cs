using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using FluentAssertions;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.VsPackage.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.Automation
{
    [TestClass]
    public class GoogleTestAdapterAutomationTests : TestsBase
    {
        private const string SolutionSettings = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <GoogleTestAdapterSettings>
    <SolutionSettings>
      <Settings>
        <WorkingDir>SolutionWorkingDir</WorkingDir>
        <AdditionalTestDiscoveryParam>-solution $(Executable)</AdditionalTestDiscoveryParam>
      </Settings>
    </SolutionSettings>
    <ProjectSettings>
      <Settings ProjectRegex=""\\Tests_gta\.exe$"">
        <WorkingDir>$(ExecutableDir)\ProjectWorkingDir</WorkingDir>
      </Settings>
    </ProjectSettings>
  </GoogleTestAdapterSettings>
</RunSettings>";

        private string _solutionSettingsFile;

        [TestInitialize]
        public void CreateSolutionSettingsFile()
        {
            _solutionSettingsFile = Path.GetTempFileName();
            File.WriteAllText(_solutionSettingsFile, SolutionSettings);
        }

        [TestCleanup]
        public void DeleteSolutionSettingsFile()
        {
            File.Delete(_solutionSettingsFile);
        }

        private GoogleTestAdapterAutomation CreateAutomation(bool initialized = true)
        {
            var globalRunSettings = new RunSettings
            {
                TestDiscoveryRegex = "",
                AdditionalTestExecutionParam = "-global",
                SkipOriginCheck = false
            };
            var mockGlobalRunSettings = new Mock<IGlobalRunSettings>();
            mockGlobalRunSettings.Setup(grs => grs.RunSettings).Returns(globalRunSettings);
            var service = new RunSettingsServiceUnderTest(mockGlobalRunSettings.Object, _solutionSettingsFile);

            return new GoogleTestAdapterAutomation(() => initialized ? service : null, "Google Test Adapter", new Version(1, 2, 3, 4));
        }

        private static Dictionary<string, object> Parse(string json)
        {
            var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            result["schemaVersion"].Should().Be(GoogleTestAdapterAutomation.SchemaVersion);
            return result;
        }

        private static IEnumerable<string> Messages(Dictionary<string, object> result, string severity)
        {
            return ((ArrayList)result["messages"]).Cast<Dictionary<string, object>>()
                .Where(m => (string)m["severity"] == severity)
                .Select(m => (string)m["message"]);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetVersion__NameAndVersion()
        {
            var result = Parse(CreateAutomation().GetVersion());

            result["name"].Should().Be("Google Test Adapter");
            result["version"].Should().Be("1.2.3.4");
            ((ArrayList)result["messages"]).Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetRunSettings_NoRunSettingsFile_GlobalAndSolutionSettings()
        {
            var result = Parse(CreateAutomation().GetRunSettings(null));

            var runSettings = (string)result["runSettings"];
            runSettings.Should().StartWith("<RunSettings>");
            runSettings.Should().Contain("<AdditionalTestExecutionParam>-global</AdditionalTestExecutionParam>");
            runSettings.Should().Contain("<WorkingDir>SolutionWorkingDir</WorkingDir>");
            runSettings.Should().Contain(@"ProjectRegex=""\\Tests_gta\.exe$""");
            Messages(result, "Error").Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetRunSettings_RunSettingsFile_ItsSettingsTakePrecedence()
        {
            string runSettingsFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(runSettingsFile, SolutionSettings.Replace("SolutionWorkingDir", "UserWorkingDir"));

                var result = Parse(CreateAutomation().GetRunSettings(runSettingsFile));

                ((string)result["runSettings"]).Should().Contain("<WorkingDir>UserWorkingDir</WorkingDir>");
            }
            finally
            {
                File.Delete(runSettingsFile);
            }
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetRunSettings_NotInitialized_ErrorMessage()
        {
            var result = Parse(CreateAutomation(initialized: false).GetRunSettings(null));

            result.Should().NotContainKey("runSettings");
            Messages(result, "Error").Should().ContainSingle().Which.Should().Contain("not been initialized");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetRunSettings_RunSettingsFileDoesNotExist_ErrorMessage()
        {
            var result = Parse(CreateAutomation().GetRunSettings(@"C:\NotExisting\My.runsettings"));

            Messages(result, "Error").Should().ContainSingle().Which.Should().Contain("My.runsettings");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetEffectiveSettings_ExecutableMatchingProjectSettings_ProjectSettingsAreUsed()
        {
            string executable = Path.GetFullPath(TestResources.Tests_DebugX86);

            var result = Parse(CreateAutomation().GetEffectiveSettings(executable));

            result["executable"].Should().Be(executable);
            ((Dictionary<string, object>)result["projectSettings"])["projectRegex"].Should().Be(@"\\Tests_gta\.exe$");
            var settings = (Dictionary<string, object>)result["settings"];
            settings[nameof(SettingsWrapper.WorkingDir)].Should().Be(@"$(ExecutableDir)\ProjectWorkingDir");
            settings[nameof(SettingsWrapper.AdditionalTestExecutionParam)].Should().Be("-global");
            settings[nameof(SettingsWrapper.ParallelTestExecution)].Should().Be(false);
            settings[nameof(SettingsWrapper.DebuggerKind)].Should().BeOfType<string>();
            var discovery = (Dictionary<string, object>)result["discovery"];
            discovery["workingDir"].Should().Be(Path.GetDirectoryName(executable) + @"\ProjectWorkingDir");
            discovery["additionalParameters"].Should().Be("-solution " + executable);
            Messages(result, "Error").Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetEffectiveSettings_ExecutableNotMatchingProjectSettings_SolutionSettingsAreUsed()
        {
            var result = Parse(CreateAutomation().GetEffectiveSettings(TestResources.LoadTests_ReleaseX86));

            result["projectSettings"].Should().BeNull();
            ((Dictionary<string, object>)result["settings"])[nameof(SettingsWrapper.WorkingDir)].Should().Be("SolutionWorkingDir");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void CheckExecutable_GoogleTestExecutable_WouldBeDiscovered()
        {
            var result = Parse(CreateAutomation().CheckExecutable(TestResources.Tests_DebugX86));

            result["exists"].Should().Be(true);
            result["isExe"].Should().Be(true);
            var trust = (Dictionary<string, object>)result["trust"];
            trust["securityZone"].Should().Be("MyComputer");
            trust["isOnNetworkDrive"].Should().Be(false);
            trust["isTrusted"].Should().Be(true);
            result["googleTestIndicator"].Should().Be(GoogleTestDiscoverer.IndicatorGoogleTestMarkers);
            ((string)result["pdb"]).Should().EndWith("Tests_gta.pdb");
            result["wouldBeDiscovered"].Should().Be(true);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void CheckExecutable_ExecutableDoesNotExist_WouldNotBeDiscovered()
        {
            var result = Parse(CreateAutomation().CheckExecutable(@"C:\NotExisting\Tests.exe"));

            result["exists"].Should().Be(false);
            result["googleTestIndicator"].Should().BeNull();
            result["wouldBeDiscovered"].Should().Be(false);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void CheckExecutable_NetworkShare_NotTrusted()
        {
            var result = Parse(CreateAutomation().CheckExecutable(@"\\gta-nonexisting-host\share\Tests.exe"));

            var trust = (Dictionary<string, object>)result["trust"];
            trust["isOnNetworkDrive"].Should().Be(true);
            trust["isTrusted"].Should().Be(false);
            result["wouldBeDiscovered"].Should().Be(false);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Write_SpecialCharactersAndValues_ValidJson()
        {
            string json = Json.Write(new JsonObject
            {
                { "string", "quote \" backslash \\ newline \n tab \t control \u0001 umlaut \u00e4" },
                { "number", 42 },
                { "bool", true },
                { "null", null },
                { "enum", DayOfWeek.Monday },
                { "list", new List<int> { 1, 2 } },
                { "dictionary", new Dictionary<string, string> { { "key", "value" } } }
            });

            var result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            result["string"].Should().Be("quote \" backslash \\ newline \n tab \t control \u0001 umlaut \u00e4");
            result["number"].Should().Be(42);
            result["bool"].Should().Be(true);
            result["null"].Should().BeNull();
            result["enum"].Should().Be("Monday");
            ((ArrayList)result["list"]).Should().Equal(1, 2);
            ((Dictionary<string, object>)result["dictionary"])["key"].Should().Be("value");
        }
    }
}
