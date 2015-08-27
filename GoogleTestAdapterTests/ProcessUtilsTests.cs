using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace GoogleTestAdapter
{
    [TestClass]
    public class ProcessUtilsTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void ReturnsOutputOfCommand()
        {
            List<string> Output = ProcessUtils.GetOutputOfCommand(MockLogger.Object, ".", "cmd.exe", "/C \"echo 2\"", false, false);

            Assert.AreEqual(1, Output.Count);
            Assert.AreEqual("2", Output[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ThrowsIfProcessFails()
        {
            ProcessUtils.GetOutputOfCommand(MockLogger.Object, ".", "cmd.exe", "/C \"exit 2\"", false, true);
        }

        [TestMethod]
        public void DoesNotThrowIfProcessFails()
        {
            ProcessUtils.GetOutputOfCommand(MockLogger.Object, ".", "cmd.exe", "/C \"exit 2\"", false, false);
        }

    }

}