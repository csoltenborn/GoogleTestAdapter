using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    public class TestExecutorSequentialTests_NativeDebugging : TestExecutorSequentialTests
    {
        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.Native);

            MockRunContext.Setup(c => c.IsBeingDebugged).Returns(true);
            SetUpMockFrameworkHandle();
        }

        protected override void SetUpMockFrameworkHandle()
        {
            MockFrameworkHandle.Setup(
                    h => h.LaunchProcessWithDebuggerAttached(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>()))
                .Returns((string filePath,
                    string workingDirectory,
                    string arguments,
                    IDictionary<string, string> environmentVariables) =>
                {
                    var processStartInfo = new ProcessStartInfo(filePath, arguments)
                    {
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    foreach (var kvp in environmentVariables)
                    {
                        processStartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                    }

                    var process = new Process {StartInfo = processStartInfo};
                    if (!process.Start())
                        throw new Exception("WTF!");
                    return process.Id;
                });
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_CrashingX64Tests_CorrectTestResults()
        {
            base.RunTests_CrashingX64Tests_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_CrashingX86Tests_CorrectTestResults()
        {
            base.RunTests_CrashingX86Tests_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_HardCrashingX86Tests_CorrectTestResults()
        {
            base.RunTests_HardCrashingX86Tests_CorrectTestResults();
        }

        #region Method stubs for code coverage

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_ExternallyLinkedX64_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX64_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_ExternallyLinkedX86Tests_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX86Tests_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_StaticallyLinkedX64Tests_CorrectTestResults()
        {
            base.RunTests_StaticallyLinkedX64Tests_CorrectTestResults();
        }

        [TestMethod]
        public override void RunTests_StaticallyLinkedX64Tests_OutputIsPrintedAtMostOnce()
        {
            base.RunTests_StaticallyLinkedX64Tests_OutputIsPrintedAtMostOnce();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_StaticallyLinkedX86Tests_CorrectTestResults()
        {
            base.RunTests_StaticallyLinkedX86Tests_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg()
        {
            base.RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WithNonexistingSetupBatch_LogsError()
        {
            base.RunTests_WithNonexistingSetupBatch_LogsError();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WithPathExtension_ExecutionOk()
        {
            base.RunTests_WithPathExtension_ExecutionOk();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning()
        {
            base.RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning()
        {
            base.RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WithoutBatches_NoLogging()
        {
            base.RunTests_WithoutBatches_NoLogging();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WithoutPathExtension_ExecutionFails()
        {
            base.RunTests_WithoutPathExtension_ExecutionFails();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_WorkingDir_IsSetCorrectly()
        {
            base.RunTests_WorkingDir_IsSetCorrectly();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_CancelingExecutorAndKillProcesses_StopsTestExecutionFaster()
        {
            base.RunTests_CancelingExecutorAndKillProcesses_StopsTestExecutionFaster();
        }

        [TestMethod]
        [TestCategory(TestMetadata.TestCategories.Integration)]
        public override void RunTests_CancelingExecutor_StopsTestExecution()
        {
            base.RunTests_CancelingExecutor_StopsTestExecution();
        }

        [TestMethod]
        public override void MemoryLeakTests_ExitCodeTest_OnlyexitCodeTestResultAndNoWarnings()
        {
            base.MemoryLeakTests_ExitCodeTest_OnlyexitCodeTestResultAndNoWarnings();
        }

        #endregion
    }
}