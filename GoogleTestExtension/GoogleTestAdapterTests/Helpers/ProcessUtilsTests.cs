using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class ProcessUtilsTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ReturnsOutputOfCommand()
        {
            List<string> output = ProcessUtils.GetOutputOfCommand(MockLogger.Object, ".", "cmd.exe", "/C \"echo 2\"", false, false, null, null);

            Assert.AreEqual(1, output.Count);
            Assert.AreEqual("2", output[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ThrowsIfProcessFails()
        {
            ProcessUtils.GetOutputOfCommand(MockLogger.Object, ".", "cmd.exe", "/C \"exit 2\"", false, true, null, null);
        }

        [TestMethod]
        public void DoesNotThrowIfProcessFails()
        {
            ProcessUtils.GetOutputOfCommand(MockLogger.Object, ".", "cmd.exe", "/C \"exit 2\"", false, false, null, null);
        }

    }

}