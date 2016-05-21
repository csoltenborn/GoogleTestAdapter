using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Framework;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class TestProcessLauncherTests : AbstractCoreTests
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void GetOutputOfCommand_WithSimpleCommand_ReturnsOutputOfCommand()
        {
            List<string> output = new TestProcessLauncher(TestEnvironment, false)
                .GetOutputOfCommand(".", "cmd.exe", "/C \"echo 2\"", false, false, null);

            output.Count.Should().Be(1);
            output[0].Should().Be("2");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetOutputOfCommand_WhenDebugging_InvokesDebuggedProcessLauncherCorrectly()
        {
            int processId = -4711;
            var mockLauncher = new Mock<IDebuggedProcessLauncher>();
            mockLauncher.Setup(l => 
                l.LaunchProcessWithDebuggerAttached(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(processId);

            new TestProcessLauncher(TestEnvironment, true)
                .Invoking(pl => pl.GetOutputOfCommand("theDir", "theCommand", "theParams", false, false, mockLauncher.Object))
                .ShouldThrow<ArgumentException>()
                .Where(e => e.Message.Contains(processId.ToString()));

            mockLauncher.Verify(l => l.LaunchProcessWithDebuggerAttached(
                It.Is<string>(s => s == "theCommand"),
                It.Is<string>(s => s == "theDir"),
                It.Is<string>(s => s == "theParams"),
                It.Is<string>(s => s == "")
                ), Times.Exactly(1));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetOutputOfCommand_ThrowsIfProcessReturnsErrorCode_Throws()
        {
            new TestProcessLauncher(TestEnvironment, false)
                .Invoking(pl => pl.GetOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", false, true, null))
                .ShouldThrow<Exception>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetOutputOfCommand_IgnoresIfProcessReturnsErrorCode_DoesNotThrow()
        {
            new TestProcessLauncher(TestEnvironment, false)
                .GetOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", false, false, null);
        }

    }

}