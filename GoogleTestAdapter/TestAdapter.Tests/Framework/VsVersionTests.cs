using System;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.TestAdapter.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    [TestClass]
    public class VsVersionTests
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void GetExecutablePath_CurrentProcess_PathOfExecutable()
        {
            Process process = Process.GetCurrentProcess();

            ParentProcessUtils.GetExecutablePath(process.Id).Should().BeEquivalentTo(process.MainModule.FileName);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetParentProcessId_CurrentProcess_ParentIsFound()
        {
            int parentId = ParentProcessUtils.GetParentProcessId(Process.GetCurrentProcess().Id);

            parentId.Should().BeGreaterThan(0);
            ParentProcessUtils.GetExecutablePath(parentId).Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetExecutablePath_NotExistingProcess_Null()
        {
            ParentProcessUtils.GetExecutablePath(int.MaxValue).Should().BeNull();
            ParentProcessUtils.GetParentProcessId(int.MaxValue).Should().Be(0);
        }

        // the tests are run by vstest.console.exe or by Visual Studio, whatever the bitness of the test host
        [TestMethod]
        [TestCategory(Unit)]
        public void VsVersion_RunByVsTest_VersionIsIdentified()
        {
            string executable = VsVersionUtils.FindVsOrVsTestConsoleExe();

            executable.Should().NotBeNull();
            Path.GetFileName(executable).Should().MatchRegex(@"(?i)^(vstest\..*|devenv)\.exe$");
            ((int)VsVersionUtils.VsVersion).Should().BeInRange((int)VsVersion.VS2017, (int)VsVersionUtils.LastSupportedVersion);
            VsVersionUtils.VsVersionDetectionError.Should().BeNull();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void NeedsToBeThrottled_UnknownVersion_False()
        {
            VsVersion.Unknown.NeedsToBeThrottled().Should().BeFalse();
            VsVersion.VS2015.NeedsToBeThrottled().Should().BeTrue();
            VsVersion.VS2026.NeedsToBeThrottled().Should().BeFalse();
        }
    }
}
