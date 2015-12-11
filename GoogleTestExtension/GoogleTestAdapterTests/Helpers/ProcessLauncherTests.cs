using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class ProcessLauncherTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ReturnsOutputOfCommand()
        {
            List<string> output = new ProcessLauncher(TestEnvironment, false).GetOutputOfCommand(".", "cmd.exe", "/C \"echo 2\"", false, false, null);

            Assert.AreEqual(1, output.Count);
            Assert.AreEqual("2", output[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ThrowsIfProcessFails()
        {
            new ProcessLauncher(TestEnvironment, false).GetOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", false, true, null);
        }

        [TestMethod]
        public void DoesNotThrowIfProcessFails()
        {
            new ProcessLauncher(TestEnvironment, false).GetOutputOfCommand(".", "cmd.exe", "/C \"exit 2\"", false, false, null);
        }

    }

}