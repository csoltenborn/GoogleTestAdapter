using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using GoogleTestAdapter.VsPackage.Helpers;
using GoogleTestAdapterUiTests;
using GoogleTestAdapterUiTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.VsPackage
{

    public abstract class AbstractConsoleIntegrationTests
    {
        protected readonly string TestAdapterDir;
        private readonly string _solutionFile;

        protected AbstractConsoleIntegrationTests()
        {
            GetDirectories(out TestAdapterDir, out _solutionFile);
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
            string arguments = CreateListDiscoverersArguments();
            string output = RunExecutableAndGetOutput(_solutionFile, arguments);
            Assert.IsTrue(output.Contains(@"executor://GoogleTestRunner/v1"));
        }

        [TestMethod]
        [TestCategory("End to end")]
        public virtual void Console_ListExecutors_ExecutorIsListed()
        {
            string arguments = CreateListExecutorsArguments();
            string output = RunExecutableAndGetOutput(_solutionFile, arguments);
            Assert.IsTrue(output.Contains(@"executor://GoogleTestRunner/v1"));
        }

        [TestMethod]
        [TestCategory("End to end")]
        public virtual void Console_ListSettingsProviders_SettingsProviderIsListed()
        {
            string arguments = CreateListSettingsProvidersArguments();
            string output = RunExecutableAndGetOutput(_solutionFile, arguments);
            Assert.IsTrue(output.Contains(@"GoogleTestAdapter"));
        }


        public static string RunExecutableAndGetOutput(string solutionFile, string arguments)
        {
            string command = VsExperimentalInstance.GetVsTestConsolePath(VsExperimentalInstance.Versions.VS2015);
            string workingDir = "";

            TestProcessLauncher launcher = new TestProcessLauncher();
            List<string> standardOut;
            List<string> standardErr;
            launcher.GetOutputStreams(workingDir, command, arguments, out standardOut, out standardErr);

            string resultString = string.Join("\n", standardOut) + "\n\n" + string.Join("\n", standardErr);
            // ReSharper disable once AssignNullToNotNullAttribute
            string baseDir = Directory.GetParent(Path.GetDirectoryName(solutionFile)).FullName;
            resultString = NormalizeOutput(resultString, baseDir);

            return resultString;
        }

        public static bool IsRunningOnBuildServer()
        {
#pragma warning disable 162
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return ResultChecker.OverwriteTestResults || Environment.GetEnvironmentVariable("APPVEYOR") != null;
#pragma warning restore 162
        }

        private static string NormalizeOutput(string resultString, string baseDir)
        {
            resultString = resultString.ReplaceIgnoreCase(baseDir, "${BaseDir}");
            resultString = Regex.Replace(resultString, @"\\(Debug|Release)\\", @"\${ConfigurationName}\");
            resultString = Regex.Replace(resultString, @"Test execution time: .*", "Test execution time: ${RunTime}");
            resultString = VS.TestExplorer.Parser.NormalizePointerInfo(resultString);
            resultString = Regex.Replace(resultString, @"Version .*\s*Copyright", "Version ${ToolVersion} Copyright");
            resultString = Regex.Replace(resultString, "Found [0-9]+ tests in executable", "Found ${NrOfTests} tests in executable");

            // exception messages are localized (thanks, MS). Add your own language here...
            resultString = resultString.Replace("   bei ", "   at ");
            resultString = Regex.Replace(resultString, @":Zeile ([0-9]+)\.", ":line $1");

            string hasCoveragePattern = @"Attachments:\n.*\.coverage\n\n";
            if (Regex.IsMatch(resultString, hasCoveragePattern))
            {
                resultString = Regex.Replace(resultString, hasCoveragePattern, "");
                resultString += "\n\nGoogle Test Adapter Coverage Marker";
            }
            else
            {
                // workaround for build server - wtf?
                hasCoveragePattern = @"Attachments:\n.*\.coverage\n";
                if (Regex.IsMatch(resultString, hasCoveragePattern))
                {
                    resultString = Regex.Replace(resultString, hasCoveragePattern, "");
                    resultString += "\nGoogle Test Adapter Coverage Marker";
                }
            }

            string noDataAdapterPattern = "Warning: Could not find diagnostic data adapter 'Code Coverage'. Make sure diagnostic data adapter is installed and try again.\n\n";
            if (Regex.IsMatch(resultString, noDataAdapterPattern))
            {
                resultString = Regex.Replace(resultString, noDataAdapterPattern, "");
                resultString += "\n\nGoogle Test Adapter Coverage Marker";
            }

            return resultString;
        }

        private string CreateListDiscoverersArguments()
        {
            return GetAdapterIntegration() + @" /ListDiscoverers /Logger:Console";
        }

        private string CreateListExecutorsArguments()
        {
            return GetAdapterIntegration() + @" /ListExecutors /Logger:Console";
        }

        private string CreateListSettingsProvidersArguments()
        {
            return GetAdapterIntegration() + @" /ListSettingsProviders /Logger:Console";
        }

    }

}