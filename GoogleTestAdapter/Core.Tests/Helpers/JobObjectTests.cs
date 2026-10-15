using System;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Common
{
    [TestClass]
    public class JobObjectTests : TestsBase
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void Terminate_ProcessInJob_ProcessIsKilled()
        {
            using (Process process = StartLongRunningProcess())
            using (JobObject job = JobObject.TryCreate(process.Handle, process.Id, MockLogger.Object))
            {
                job.Should().NotBeNull();

                job.Terminate();

                process.WaitForExit(5000).Should().BeTrue();
            }
            MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void Dispose_ProcessInJob_ProcessKeepsRunning()
        {
            using (Process process = StartLongRunningProcess())
            {
                try
                {
                    JobObject job = JobObject.TryCreate(process.Handle, process.Id, MockLogger.Object);
                    job.Should().NotBeNull();

                    job.Dispose();
                    job.Terminate();

                    process.WaitForExit(500).Should().BeFalse();
                }
                finally
                {
                    process.Kill();
                }
            }
            MockLogger.Verify(l => l.DebugWarning(It.IsAny<string>()), Times.Never);
        }

        private static Process StartLongRunningProcess()
        {
            return Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "ping.exe"), "-n 30 127.0.0.1")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }
}
