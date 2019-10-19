using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Helpers
{
    [TestClass]
    public class DotNetProcessExecutorTests : ProcessExecutorTests
    {
        [TestInitialize]
        public void Setup()
        {
            ProcessExecutor = new DotNetProcessExecutor(true, MockLogger.Object);
        }

        [TestCleanup]
        public override void Teardown()
        {
            base.Teardown();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_PingLocalHost()
        {
            Test_ExecuteProcessBlocking_PingLocalHost();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SampleTests()
        {
            Test_ExecuteProcessBlocking_SampleTests();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_WithSimpleCommand_ReturnsOutputOfCommand()
        {
            Test_WithSimpleCommand_ReturnsOutputOfCommand();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_IgnoresIfProcessReturnsErrorCode_DoesNotThrow()
        {
            Test_WithSimpleCommand_ReturnsOutputOfCommand();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SetEnvVariable_EnvVariableIsSet()
        {
            Test_WithEnvSetting_EnvVariableIsSet();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_SetExistingEnvVariable_EnvVariableIsOverridden()
        {
            Test_WithOverridingEnvSetting_EnvVariableHasNewValue();
        }
    }

}