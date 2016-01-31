using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapterUiTests.Helpers;
using GoogleTestAdapter.VsPackage.Helpers;

namespace GoogleTestAdapterUiTests
{

    public abstract class AbstractConsoleIntegrationTests
    {
        protected readonly string testAdapterDir;
        private readonly string solutionFile;

        public AbstractConsoleIntegrationTests()
        {
            GetDirectories(out testAdapterDir, out solutionFile);
        }

        protected abstract string GetAdapterIntegration();

        public static void GetDirectories(out string testAdapterDir, out string testSolutionFile)
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestAdapter\\VsPackage.Tests.*\\bin\\(Debug|Release)\\GoogleTestAdapter.VsPackage.Tests.*.dll$");
            Assert.IsTrue(match.Success);
            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            testAdapterDir = Path.Combine(basePath, @"GoogleTestAdapter\TestAdapter\bin", debugOrRelease);
            testSolutionFile = Path.Combine(basePath, @"SampleTests\SampleTests.sln");
        }


        [TestMethod]
        [TestCategory("End to end")]
        public virtual void Console_ListDiscoverers_DiscovererIsListed()
        {
            string arguments = CreateListDiscoverersArguments(testAdapterDir);
            string output = RunExecutableAndGetOutput(solutionFile, arguments);
            Assert.IsTrue(output.Contains(@"executor://GoogleTestRunner/v1"));
        }

        [TestMethod]
        [TestCategory("End to end")]
        public virtual void Console_ListExecutors_ExecutorIsListed()
        {
            string arguments = CreateListExecutorsArguments(testAdapterDir);
            string output = RunExecutableAndGetOutput(solutionFile, arguments);
            Assert.IsTrue(output.Contains(@"executor://GoogleTestRunner/v1"));
        }

        [TestMethod]
        [TestCategory("End to end")]
        public virtual void Console_ListSettingsProviders_SettingsProviderIsListed()
        {
            string arguments = CreateListSettingsProvidersArguments(testAdapterDir);
            string output = RunExecutableAndGetOutput(solutionFile, arguments);
            Assert.IsTrue(output.Contains(@"GoogleTestAdapter"));
        }


        public static string RunExecutableAndGetOutput(string solutionFile, string arguments, [CallerMemberName] string testCaseName = null)
        {
            string command = VsExperimentalInstance.GetVsTestConsolePath(VsExperimentalInstance.Versions.VS2015);
            string workingDir = "";

            ProcessLauncher launcher = new ProcessLauncher(new ConsoleLogger());
            int resultCode;
            List<string> output = launcher.GetOutputOfCommand(workingDir, command, arguments, false, false, out resultCode);

            string resultString = string.Join("\n", output);
            resultString = resultString.ReplaceIgnoreCase(Path.GetDirectoryName(solutionFile), "${SolutionDir}");
            resultString = Regex.Replace(resultString, @"Test execution time: .*", "Test execution time: ${RunTime}");
            resultString = VS.TestExplorer.Parser.NormalizePointerInfo(resultString);
            resultString = Regex.Replace(resultString, @"Version .*\s*Copyright", "Version ${ToolVersion} Copyright");

            return resultString;
        }

        private string CreateListDiscoverersArguments(string testAdapterDir)
        {
            return GetAdapterIntegration() + @" /ListDiscoverers /Logger:Console";
        }

        private string CreateListExecutorsArguments(string testAdapterDir)
        {
            return GetAdapterIntegration() + @" /ListExecutors /Logger:Console";
        }

        private string CreateListSettingsProvidersArguments(string testAdapterDir)
        {
            return GetAdapterIntegration() + @" /ListSettingsProviders /Logger:Console";
        }

    }

}