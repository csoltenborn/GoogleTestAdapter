using System.Runtime.InteropServices;

namespace GoogleTestAdapter.VsPackage.Automation
{
    /// <summary>
    /// GTA's automation object, available as DTE.GetObject("GoogleTestAdapter"), e.g. for scripts and MCP servers. All
    /// methods return JSON objects with members "schemaVersion" (incremented on incompatible changes) and "messages"
    /// (the warnings and errors which occurred, each with "severity" and "message"); they do not throw.
    /// </summary>
    [ComVisible(true)]
    [Guid("055B4C3E-034E-447E-ABBC-F05650F4A849")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IGoogleTestAdapterAutomation
    {
        /// <returns>"name" and "version" of the installed GTA</returns>
        string GetVersion();

        /// <summary>
        /// The run settings GTA provides to Test Explorer for the open solution, i.e. GTA's options, merged with the
        /// solution's settings file and the given run settings file.
        /// </summary>
        /// <param name="runSettingsFile">a .runsettings file (as selected in Test Explorer), or null or empty</param>
        /// <returns>"runSettings": the run settings XML, e.g. for vstest.console's /Settings option (Test Explorer
        /// adds settings of Visual Studio and of other test adapters, which are not contained)</returns>
        string GetRunSettings(string runSettingsFile);

        /// <summary>
        /// The settings GTA uses for an executable of the open solution: GTA's options and the settings of the
        /// solution's settings file, overridden by the project settings matching the executable.
        /// </summary>
        /// <returns>"executable", "projectSettings" ("projectRegex" of the matching project settings, or null),
        /// "settings" (by name of the setting, see GTA's documentation), and "discovery" (the values GTA resolves for
        /// test discovery: "workingDir", "pathExtension", "environmentVariables", "additionalParameters")</returns>
        string GetEffectiveSettings(string executable);

        /// <summary>
        /// Whether GTA would discover the tests of an executable, and why (the executable is not run; Test Explorer
        /// asks GTA for the outputs of the solution's projects only).
        /// </summary>
        /// <returns>"executable", "exists", "isExe", "trust" ("securityZone", "isOnNetworkDrive", "skipOriginCheck",
        /// "isTrusted"), "googleTestIndicator" (what makes it a Google Test executable: "IndicatorFile",
        /// "GoogleTestDll", "GoogleTestFunctions", "GoogleTestMarkers", or "TestDiscoveryRegex"; null if it is not one),
        /// "testDiscoveryRegex", "pdb" (null if not found), and "wouldBeDiscovered"</returns>
        string CheckExecutable(string executable);

        /// <summary>
        /// Starts discovering the tests of an executable in the background (discovery runs the executable, which
        /// takes up to the discovery timeout; COM calls would block Visual Studio's UI thread meanwhile). The
        /// executable is only run if GTA would discover its tests (see CheckExecutable()).
        /// </summary>
        /// <returns>"discoveryId" for GetTestDiscoveryResult(), and "executable"</returns>
        string StartTestDiscovery(string executable);

        /// <summary>
        /// The state and result of a test discovery started by StartTestDiscovery(); the results of the latest 32
        /// discoveries are kept.
        /// </summary>
        /// <returns>"discoveryId", "status" ("Running", "Completed", "Failed", or "Unknown" for unknown ids),
        /// "executable", "durationInSeconds", "discoveryMessages" (the warnings and errors of the discovery so far),
        /// and, if completed, "tests": "fullyQualifiedName", "displayName", "kind" ("Test", "TearDownTestSuite", or
        /// "ExitCode"), "codeFilePath" and "lineNumber" (if known), "namespace", "testClass", "testGroup", "traits"
        /// ("name", "value"), and "testProperties" (the CTest test properties "workingDirectory", "environment",
        /// "labels", "timeoutInSeconds", and "disabled"; null if there are none)</returns>
        string GetTestDiscoveryResult(string discoveryId);
    }
}
