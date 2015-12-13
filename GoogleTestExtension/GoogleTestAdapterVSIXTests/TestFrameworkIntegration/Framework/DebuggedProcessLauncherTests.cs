using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapterVSIX.TestFrameworkIntegration.Framework
{
    [TestClass]
    public class DebuggedProcessLauncherTests : AbstractVSIXTests
    {

        [TestMethod]
        public void DebuggedProcessLauncherTests_CalledWithParameters_InvokesFrameworkhandleCorrectly()
        {
            DebuggedProcessLauncher launcher = new DebuggedProcessLauncher(MockFrameworkHandle.Object);

            launcher.LaunchProcessWithDebuggerAttached("theCommand", "theDir", "theParams");

            MockFrameworkHandle.Verify(fh => fh.LaunchProcessWithDebuggerAttached(
                It.Is<string>(s => s == "theCommand"),
                It.Is<string>(s => s == "theDir"),
                It.Is<string>(s => s == "theParams"),
                It.IsAny<IDictionary<string, string>>()
                ), Times.Exactly(1));
        }

    }

}