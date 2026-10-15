using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
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
        public void Cancel_BeforeExecution_ProcessIsNotStarted()
        {
            Test_CancelBeforeExecution_ProcessIsNotStarted();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void Cancel_ProcessWithChildProcess_KillsProcessTree()
        {
            Test_Cancel_KillsProcessTree();
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

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Unit)]
        public void ExecuteProcessBlocking_OutputOnStandardOutputAndError_LinesAreReportedSequentially()
        {
            const int nrOfLines = 2000;
            var lines = new List<string>();
            int nrOfActiveCalls = 0;
            bool calledConcurrently = false;

            void ReportOutputLine(string line)
            {
                if (Interlocked.Increment(ref nrOfActiveCalls) > 1)
                    calledConcurrently = true;
                Thread.SpinWait(1000);
                lines.Add(line);
                Interlocked.Decrement(ref nrOfActiveCalls);
            }

            string command = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            int exitCode = new DotNetProcessExecutor(false, MockLogger.Object).ExecuteCommandBlocking(
                command, $"/c for /l %i in (1,1,{nrOfLines}) do @(echo out%i& echo err%i 1>&2)", ".", null,
                new Dictionary<string, string>(), ReportOutputLine);

            exitCode.Should().Be(0);
            calledConcurrently.Should().BeFalse();
            lines.Count(l => l.StartsWith("out")).Should().Be(nrOfLines);
            lines.Count(l => l.StartsWith("err")).Should().Be(nrOfLines);
        }
    }

}