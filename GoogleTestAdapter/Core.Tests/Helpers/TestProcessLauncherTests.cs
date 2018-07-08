using System.Collections.Generic;
using FluentAssertions;
using GoogleTestAdapter.ProcessExecution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter.Tests.Common;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class TestProcessLauncherTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void GetOutputOfCommand_WithSimpleCommand_ReturnsOutputOfCommand()
        {
            var output = new List<string>();
            var executor = new FrameworkProcessExecutor(true, MockLogger.Object);
            int returnCode = executor.ExecuteCommandBlocking("cmd.exe", "/C \"echo 2\"", ".", "", line => output.Add(line));

            returnCode.Should().Be(0);
            output.Count.Should().Be(1);
            output[0].Should().Be("2");
        }

        //[TestMethod]
        //[TestCategory(Unit)]
        //public void GetOutputOfCommand_WhenDebugging_InvokesDebuggedProcessLauncherCorrectly()
        //{
        //    int processId = -4711;
        //    var mockLauncher = new Mock<IDebuggedProcessLauncher>();
        //    mockLauncher.Setup(l => 
        //        l.LaunchProcessWithDebuggerAttached(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        //        .Returns(processId);

        //    new TestProcessLauncher(TestEnvironment.Logger, TestEnvironment.Options, true)
        //        .Invoking(pl => pl.GetOutputOfCommand("theDir", "theCommand", "theParams", false, false, mockLauncher.Object, new List<string>()))
        //        .ShouldThrow<ArgumentException>()
        //        .Where(e => e.Message.Contains(processId.ToString()));

        //    mockLauncher.Verify(l => l.LaunchProcessWithDebuggerAttached(
        //        It.Is<string>(s => s == "theCommand"),
        //        It.Is<string>(s => s == "theDir"),
        //        It.Is<string>(s => s == "theParams"),
        //        It.Is<string>(s => s == "")
        //        ), Times.Exactly(1));
        //}

        [TestMethod]
        [TestCategory(Unit)]
        public void GetOutputOfCommand_IgnoresIfProcessReturnsErrorCode_DoesNotThrow()
        {
            var executor = new FrameworkProcessExecutor(false, MockLogger.Object);
            executor.ExecuteCommandBlocking("cmd.exe", "/C \"echo 2\"", ".", "", line => { });
        }

    }

}