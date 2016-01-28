using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.VsPackage;
using GoogleTestAdapterUiTests.Helpers;
using GoogleTestAdapter.VsPackage.Helpers;

namespace GoogleTestAdapterUiTests
{

    [TestClass]
    public class ConsoleTests
    {
        private readonly string testAdapterDir;
        private readonly string solutionFile;

        public ConsoleTests()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestAdapter\\VsPackage.Tests\\bin\\(Debug|Release)\\GoogleTestAdapter.VsPackage.Tests.dll$");
            Assert.IsTrue(match.Success);
            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            testAdapterDir = Path.Combine(basePath, @"GoogleTestAdapter\TestAdapter\bin", debugOrRelease);
            solutionFile = Path.Combine(basePath, @"SampleTests\SampleTests.sln");
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_ListDiscoverers_DiscovererIsListed()
        {
            string arguments = CreateListDiscoverersArguments(testAdapterDir);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_ListExecutors_ExecutorIsListed()
        {
            string arguments = CreateListExecutorsArguments(testAdapterDir);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_ListSettingsProviders_SettingsProviderIsListed()
        {
            string arguments = CreateListSettingsProvidersArguments(testAdapterDir);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_SampleTests_AreListedCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.SampleTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_SampleTests_AreRunCorrectly()
        {
            string arguments = CreateRunTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.SampleTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_HardCrashingSampleTests_AreListedCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.HardCrashingSampleTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_HardCrashingSampleTests_AreRunCorrectly()
        {
            string arguments = CreateRunTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.HardCrashingSampleTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_X86StaticallyLinked_AreListedCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X86StaticallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_X86StaticallyLinked_AreRunCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X86StaticallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_X86ExternallyLinked_AreListedCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X86ExternallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod]
        [TestCategory("End to end")]
        public void Console_X86ExternallyLinked_AreRunCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X86ExternallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod, Ignore]
        [TestCategory("End to end")]
        public void Console_X64StaticallyLinked_AreListedCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X64StaticallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod, Ignore]
        [TestCategory("End to end")]
        public void Console_X64StaticallyLinked_AreRunCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X64StaticallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod, Ignore]
        [TestCategory("End to end")]
        public void Console_X64ExternallyLinked_AreListedCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X64ExternallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }

        [TestMethod, Ignore]
        [TestCategory("End to end")]
        public void Console_X64ExternallyLinked_AreRunCorrectly()
        {
            string arguments = CreateListTestsArguments(testAdapterDir, AbstractGoogleTestExtensionTests.X64ExternallyLinkedTests);
            LaunchVsTestConsole(arguments);
        }


        private void LaunchVsTestConsole(string arguments, [CallerMemberName] string testCaseName = null)
        {
            string command = VsExperimentalInstance.GetVsTestConsolePath(VsExperimentalInstance.Versions.VS2015);
            string workingDir = "";

            ProcessLauncher launcher = new ProcessLauncher(new ConsoleLogger());
            int resultCode;
            List<string> output = launcher.GetOutputOfCommand(workingDir, command, arguments, false, false, out resultCode);

            string resultString = string.Join("\n", output);
            resultString = resultString.ReplaceIgnoreCase(Path.GetDirectoryName(solutionFile), "${SolutionDir}");
            resultString = Regex.Replace(resultString, @"Test execution time: .*", "Test execution time: $${RunTime}");
            resultString = Regex.Replace(resultString, "([0-9A-F]{8}){1,2} pointing to", "${MemoryLocation} pointing to");

            new ResultChecker().CheckResults(resultString, GetType().Name, testCaseName);
        }

        private string CreateRunTestsArguments(string testAdapterDir, string executable)
        {
            return @"/TestAdapterPath:" + testAdapterDir + " " + executable + @" /Logger:Console";
        }

        private string CreateListTestsArguments(string testAdapterDir, string executable)
        {
            return @"/TestAdapterPath:" + testAdapterDir + @" /ListTests:" + executable + @" /Logger:Console";
        }

        private string CreateListDiscoverersArguments(string testAdapterDir)
        {
            return @"/TestAdapterPath:" + testAdapterDir + @" /ListDiscoverers /Logger:Console";
        }

        private string CreateListExecutorsArguments(string testAdapterDir)
        {
            return @"/TestAdapterPath:" + testAdapterDir + @" /ListExecutors /Logger:Console";
        }

        private string CreateListSettingsProvidersArguments(string testAdapterDir)
        {
            return @"/TestAdapterPath:" + testAdapterDir + @" /ListSettingsProviders /Logger:Console";
        }

    }

}