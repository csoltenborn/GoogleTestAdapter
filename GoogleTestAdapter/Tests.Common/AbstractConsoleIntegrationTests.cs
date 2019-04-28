// This file has been modified by Microsoft on 6/2017.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Tests.Common
{

    public abstract class AbstractConsoleIntegrationTests
    {
        protected readonly string TestAdapterDir;
        private readonly string _solutionFile;

        protected AbstractConsoleIntegrationTests()
        {
            GetDirectories(out TestAdapterDir, out _solutionFile);
        }

        [ClassInitialize]
        public void Setup()
        {
            // workaround for first test failing
            string arguments = CreateListDiscoverersArguments();
            RunExecutableAndGetOutput(_solutionFile, arguments);
        }

        protected abstract string GetAdapterIntegration();

        public static string GetLogger()
        {
            return "/Logger:Trx ";
        }

        public static void GetDirectories(out string testAdapterDir, out string testSolutionFile)
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Assert.IsNotNull(testDll);
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestAdapter\\(Debug|Release)\\VsPackage.Tests.*\\GoogleTestAdapter.Tests.Common.dll$");
            match.Success.Should().BeTrue();
            string binariesPath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            testAdapterDir = Path.Combine(binariesPath, "GoogleTestAdapter", debugOrRelease, "Packaging.GTA");
            testSolutionFile = Path.Combine(binariesPath, @"..\..\SampleTests\SampleTests.sln");
        }


        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.EndToEnd)]
        public virtual void Console_ListDiscoverers_DiscovererIsListed()
        {
            string arguments = CreateListDiscoverersArguments();
            string output = RunExecutableAndGetOutput(_solutionFile, arguments);
            if (output.ToLower().Contains("googletestrunner"))
                Assert.Fail("VS test framework appears to have been fixed :-) - enable test!");
            else
                Assert.Inconclusive("skipped until vs test framework is fixed...");
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.EndToEnd)]
        public virtual void Console_ListExecutors_ExecutorIsListed()
        {
            string arguments = CreateListExecutorsArguments();
            string output = RunExecutableAndGetOutput(_solutionFile, arguments);
            if (output.ToLower().Contains("googletestrunner"))
                Assert.Fail("VS test framework appears to have been fixed :-) - enable test!");
            else
                Assert.Inconclusive("skipped until vs test framework is fixed...");
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.EndToEnd)]
        public virtual void Console_ListSettingsProviders_SettingsProviderIsListed()
        {
            string arguments = CreateListSettingsProvidersArguments();
            string output = RunExecutableAndGetOutput(_solutionFile, arguments);
            if (output.ToLower().Contains("googletestadapter"))
                Assert.Fail("VS test framework appears to have been fixed :-) - enable test!");
            else
                Assert.Inconclusive("skipped until vs test framework is fixed...");
        }


        public static string RunExecutableAndGetOutput(string solutionFile, string arguments)
        {
            string command = TestResources.GetVsTestConsolePath(TestMetadata.VersionUnderTest);
            string workingDir = "";

            var launcher = new TestProcessLauncher();
            launcher.GetOutputStreams(workingDir, command, arguments, out List<string> standardOut, out var standardErr);

            string resultString = string.Join("\n", standardOut) + "\n\n" + string.Join("\n", standardErr);
            // ReSharper disable once AssignNullToNotNullAttribute
            string baseDir = Directory.GetParent(Path.GetDirectoryName(solutionFile)).FullName;
            resultString = NormalizeOutput(resultString, baseDir);

            return resultString;
        }

        private static string NormalizeOutput(string resultString, string baseDir)
        {
            resultString = resultString.ReplaceIgnoreCase(baseDir, "${BaseDir}");
            resultString = Regex.Replace(resultString, @"\\(Debug|Release)\\", @"\${ConfigurationName}\");
            resultString = Regex.Replace(resultString, @"Test execution time: .*", "Test execution time: ${RunTime}");
            resultString = TestResources.NormalizePointerInfo(resultString);
            resultString = Regex.Replace(resultString, @"Version .*\s*Copyright", "Version ${ToolVersion} Copyright");
            resultString = Regex.Replace(resultString, "Found [0-9]+ tests in executable", "Found ${NrOfTests} tests in executable");

            // exception messages are localized (thanks, MS). Add your own language here...
            resultString = resultString.Replace("   bei ", "   at ");
            resultString = Regex.Replace(resultString, @":Zeile ([0-9]+)\.", ":line $1");

            string testExecutionCompletedPattern = @".*Test execution completed, overall duration: .*\n";
            if (Regex.IsMatch(resultString, testExecutionCompletedPattern))
            {
                resultString = Regex.Replace(resultString, testExecutionCompletedPattern, "");
                resultString += "\n\nTest execution completed, overall duration: ${OverallDuration}\n";
            }

            string coveragePattern = @"Attachments:\n.*\.coverage\n\n";
            if (Regex.IsMatch(resultString, coveragePattern))
            {
                resultString = Regex.Replace(resultString, coveragePattern, "");
                resultString += "\n\nGoogle Test Adapter Coverage Marker";
            }
            else
            {
                // workaround for build server
                coveragePattern = @"Attachments:\n.*\.coverage\n";
                if (Regex.IsMatch(resultString, coveragePattern))
                {
                    resultString = Regex.Replace(resultString, coveragePattern, "");
                    resultString += "\nGoogle Test Adapter Coverage Marker";
                }
            }

            string noDataAdapterPattern = "Warning: Could not find diagnostic data adapter 'Code Coverage'. Make sure diagnostic data adapter is installed and try again.\n\n";
            if (Regex.IsMatch(resultString, noDataAdapterPattern))
            {
                resultString = Regex.Replace(resultString, noDataAdapterPattern, "");
                resultString += "\n\nGoogle Test Adapter Coverage Marker";
            }

            string emptyLinePattern = @"\n\n";
            while (Regex.IsMatch(resultString, emptyLinePattern))
            {
                resultString = Regex.Replace(resultString, emptyLinePattern, "\n");
            }

            return resultString;
        }

        private string CreateListDiscoverersArguments()
        {
            return GetAdapterIntegration() + @" /ListDiscoverers " + GetLogger();
        }

        private string CreateListExecutorsArguments()
        {
            return GetAdapterIntegration() + @" /ListExecutors " + GetLogger();
        }

        private string CreateListSettingsProvidersArguments()
        {
            return GetAdapterIntegration() + @" /ListSettingsProviders " + GetLogger();
        }

    }

}