using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    [TestClass]
    public class DebuggedProcessLauncherTests : TestAdapterTestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void LaunchProcessWithDebuggerAttached_WithParameters_PassedInfoToFrameworkHandleCorrectly()
        {
            DebuggedProcessLauncher launcher = new DebuggedProcessLauncher(MockFrameworkHandle.Object);

            launcher.LaunchProcessWithDebuggerAttached("theCommand", "theDir", "theParams", @"C:\test");

            MockFrameworkHandle.Verify(fh => 
                fh.LaunchProcessWithDebuggerAttached(
                    It.Is<string>(s => s == "theCommand"),
                    It.Is<string>(s => s == "theDir"),
                    It.Is<string>(s => s == "theParams"),
                    It.Is<IDictionary<string, string>>(d => d.ContainsKey("PATH") && d["PATH"].StartsWith(@"C:\test;"))
                ), 
                Times.Exactly(1));
        }

    }

}