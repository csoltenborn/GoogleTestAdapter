// This file has been modified by Microsoft on 7/2017.

using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using Moq;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{

    [TestClass]
    public class TestDiscovererTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_WithDefaultRegex_RegistersFoundTestsAtDiscoverySink()
        {
            CheckForDiscoverySinkCalls(TestResources.NrOfTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_WithCustomNonMatchingRegex_DoesNotFindTests()
        {
            CheckForDiscoverySinkCalls(0, "NoMatchAtAll");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_CrashingExecutable_CrashIsLogged()
        {
            RunExecutableAndCheckLogging(TestResources.AlwaysCrashingExe,
                () => MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Could not list test cases of executable"))),
                    Times.Once));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_FailingExecutable_ExitCodeIsLogged()
        {
            RunExecutableAndCheckLogging(TestResources.AlwaysFailingExe,
                () => MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("executing process failed with return code 4711"))),
                    Times.Once));
        }

        private void MarkUntrusted(string path)
        {
            using (var handle = NativeMethods.CreateFileW(path + ":Zone.Identifier", NativeMethods.GENERIC_WRITE, 0, IntPtr.Zero,
                NativeMethods.CREATE_NEW, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                using (var stream = new FileStream(handle, FileAccess.Write))
                {
                    var data = Encoding.ASCII.GetBytes("[ZoneTransfer]\r\nZoneId=3");
                    stream.Write(data, 0, data.Length);
                }
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void DiscoverTests_UntrustedExecutable_IsNotRun()
        {
            var SemPath = Path.Combine(Directory.GetCurrentDirectory(), "SemaphoreExe.sem");
            var Temp1Exe = Path.Combine(Path.GetDirectoryName(TestResources.SemaphoreExe), "Temp1.exe");
            var Temp2Exe = Path.Combine(Path.GetDirectoryName(TestResources.SemaphoreExe), "Temp2.exe");

            // Verify baseline
            try
            {
                File.Copy(TestResources.SemaphoreExe, Temp1Exe);
                File.Exists(SemPath).Should().BeFalse();
                RunExecutableAndCheckLogging(Temp1Exe,
                    () => MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("executing process failed with return code 143"))),
                        Times.Once));
                File.Exists(SemPath).Should().BeTrue("because exe should have been run");
            }
            finally
            {
                File.Delete(SemPath);
                File.Delete(Temp1Exe);
            }

            // Verify untrusted exe is not run
            try
            {
                File.Copy(TestResources.SemaphoreExe, Temp2Exe);
                MarkUntrusted(Temp2Exe);
                File.Exists(SemPath).Should().BeFalse();
                RunExecutableAndCheckLogging(Temp2Exe,
                    () => MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("was blocked to help protect"))),
                        Times.Once));
                File.Exists(SemPath).Should().BeFalse("because exe should not have been run");
            }
            finally
            {
                File.Delete(SemPath);
                File.Delete(Temp2Exe);
            }
        }

        private void RunExecutableAndCheckLogging(string executable, Action verify)
        {
            var mockDiscoveryContext = new Mock<IDiscoveryContext>();
            var mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
            var mockVsLogger = new Mock<IMessageLogger>();
            MockOptions.Setup(o => o.TestDiscoveryRegex).Returns(() => ".*");

            var discoverer = new TestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
            discoverer.DiscoverTests(executable.Yield(), mockDiscoveryContext.Object, mockVsLogger.Object,
                mockDiscoverySink.Object);

            verify();
        }

        private void CheckForDiscoverySinkCalls(int expectedNrOfTests, string customRegex = null)
        {
            string dir = Utils.GetTempDirectory();
            try
            {
                string targetFile = Path.Combine(dir, "MyTests.exe");
                File.Copy(TestResources.Tests_DebugX86, targetFile);

                Mock<IDiscoveryContext> mockDiscoveryContext = new Mock<IDiscoveryContext>();
                Mock<ITestCaseDiscoverySink> mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
                MockOptions.Setup(o => o.TestDiscoveryRegex).Returns(() => customRegex);

                TestDiscoverer discoverer = new TestDiscoverer(TestEnvironment.Logger, TestEnvironment.Options);
                Mock<IMessageLogger> mockVsLogger = new Mock<IMessageLogger>();
                discoverer.DiscoverTests(targetFile.Yield(), mockDiscoveryContext.Object, mockVsLogger.Object, mockDiscoverySink.Object);

                mockDiscoverySink.Verify(h => h.SendTestCase(It.IsAny<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>()), Times.Exactly(expectedNrOfTests));
            }
            finally
            {
                Utils.DeleteDirectory(dir);
            }
        }

    }

    static class NativeMethods
    {
        public const int GENERIC_WRITE = 1073741824;
        public const int CREATE_NEW = 1;

        [DllImport("kernel32.dll")]
        public static extern SafeFileHandle CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,uint dwFlagsAndAttributes, IntPtr hTemplateFile);
    }

}