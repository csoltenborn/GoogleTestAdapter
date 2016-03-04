using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Framework;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class TestProcessLauncherTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void GetOutputOfCommand_WithSimpleCommand_ReturnsOutputOfCommand()
        {
            List<string> output = new TestProcessLauncher(TestEnvironment, false)
                .GetOutputOfCommand(".", "cmd.exe", "/C \"echo 2\"", false, false, null);

            Assert.AreEqual(1, output.Count);
            Assert.AreEqual("2", output[0]);
        }

        [TestMethod]
        public void GetOutputOfCommand_WhenDebugging_InvokesDebuggedProcessLauncherCorrectly()
        {
            int processId = -4711;
            Mock<IDebuggedProcessLauncher> mockLauncher = new Mock<IDebuggedProcessLauncher>();
            mockLauncher.Setup(l => l.LaunchProcessWithDebuggerAttached(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(processId);

            try
            {
                new TestProcessLauncher(TestEnvironment, true)
                    .GetOutputOfCommand("theDir", "theCommand", "theParams", false, false, mockLauncher.Object);
                Assert.Fail();
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains(processId.ToString()));
            }

            mockLauncher.Verify(l => l.LaunchProcessWithDebuggerAttached(
                It.Is<string>(s => s == "theCommand"),
                It.Is<string>(s => s == "theDir"),
                It.Is<string>(s => s == "theParams"),
                It.Is<string>(s => s == "")
                ), Times.Exactly(1));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void GetOutputOfCommand_ThrowsIfProcessReturnsErrorCode_Throws()
        {
            new TestProcessLauncher(TestEnvironment, false)
                .GetOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", false, true, null);
        }

        [TestMethod]
        public void GetOutputOfCommand_IgnoresIfProcessReturnsErrorCode_DoesNotThrow()
        {
            new TestProcessLauncher(TestEnvironment, false)
                .GetOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", false, false, null);
        }

    }

}