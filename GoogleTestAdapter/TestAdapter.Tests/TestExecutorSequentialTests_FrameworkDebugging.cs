using System;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VsTestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    [TestClass]
    // ReSharper disable once InconsistentNaming
    public class TestExecutorSequentialTests_FrameworkDebugging : TestExecutorSequentialTests
    {
        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);

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
        [TestCategory(Integration)]
        public override void RunTests_CrashingX64Tests_CorrectTestResults()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.DoNotReport);

            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX64, 0, 0, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CrashingX64Tests_CorrectTestResults_ReportAsFailed()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.ReportAsFailed);

            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX64, 0, 6, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CrashingX64Tests_CorrectTestResults_ReportAsSkipped()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.ReportAsSkipped);

            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX64, 0, 0, 0, 6);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CrashingX64Tests_CorrectTestResults_ReportAsNotFound()
        {
            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX64, 0, 0, 0, nrOfNotFoundTests: 6);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_CrashingX86Tests_CorrectTestResults()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.DoNotReport);

            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX86, 0, 0, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CrashingX86Tests_CorrectTestResults_ReportAsFailed()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.ReportAsFailed);

            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX86, 0, 6, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CrashingX86Tests_CorrectTestResults_ReportAsSkipped()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.ReportAsSkipped);

            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX86, 0, 0, 0, 6);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_CrashingX86Tests_CorrectTestResults_ReportAsNotFound()
        {
            RunAndVerifyTests(TestResources.CrashingTests_ReleaseX86, 0, 0, 0, nrOfNotFoundTests: 6);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_HardCrashingX86Tests_CorrectTestResults()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.DoNotReport);

            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);
            executor.RunTests(TestResources.CrashingTests_DebugX86.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(0, 0, 0, 0, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_HardCrashingX86Tests_CorrectTestResults_ReportAsFailed()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.ReportAsFailed);
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);
            executor.RunTests(TestResources.CrashingTests_DebugX86.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(0, 6, 0, 0, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_HardCrashingX86Tests_CorrectTestResults_ReportAsSkipped()
        {
            MockOptions.Setup(o => o.MissingTestsReportMode).Returns(MissingTestsReportMode.ReportAsSkipped);
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);
            executor.RunTests(TestResources.CrashingTests_DebugX86.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(0, 0, 0, 6, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_HardCrashingX86Tests_CorrectTestResults_ReportAsNotFound()
        {
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, MockDebuggerAttacher.Object);
            executor.RunTests(TestResources.CrashingTests_DebugX86.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(0, 0, 0, 0, 6);
        }

        #region Method stubs for code coverage

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_ExternallyLinkedX64_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX64_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_ExternallyLinkedX86Tests_CorrectTestResults()
        {
            base.RunTests_ExternallyLinkedX86Tests_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(Integration)]
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
        [TestCategory(Integration)]
        public override void RunTests_StaticallyLinkedX86Tests_CorrectTestResults()
        {
            base.RunTests_StaticallyLinkedX86Tests_CorrectTestResults();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg()
        {
            base.RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WithNonexistingSetupBatch_LogsError()
        {
            base.RunTests_WithNonexistingSetupBatch_LogsError();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WithPathExtension_ExecutionOk()
        {
            base.RunTests_WithPathExtension_ExecutionOk();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning()
        {
            base.RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning()
        {
            base.RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WithoutBatches_NoLogging()
        {
            base.RunTests_WithoutBatches_NoLogging();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WithoutPathExtension_ExecutionFails()
        {
            base.RunTests_WithoutPathExtension_ExecutionFails();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_WorkingDir_IsSetCorrectly()
        {
            base.RunTests_WorkingDir_IsSetCorrectly();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_CancelingExecutorAndKillProcesses_StopsTestExecutionFaster()
        {
            base.RunTests_CancelingExecutorAndKillProcesses_StopsTestExecutionFaster();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void RunTests_CancelingExecutor_StopsTestExecution()
        {
            base.RunTests_CancelingExecutor_StopsTestExecution();
        }

        [TestMethod]
        public override void RunTests_ExitCodeTest_PassingTestResultIsProduced()
        {
            base.RunTests_ExitCodeTest_PassingTestResultIsProduced();
        }

        [TestMethod]
        public override void RunTests_ExitCodeTest_FailingTestResultIsProduced()
        {
            base.RunTests_ExitCodeTest_FailingTestResultIsProduced();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void MemoryLeakTests_FailingWithLeaks_CorrectResult()
        {
            bool outputAvailable = MockOptions.Object.DebuggerKind > DebuggerKind.VsTestFramework ||
                                   !MockRunContext.Object.IsBeingDebugged;
            RunMemoryLeakTest(TestResources.LeakCheckTests_DebugX86, "memory_leaks.failing_and_leaking", VsTestOutcome.Failed, VsTestOutcome.Failed,
                msg => msg.Contains("Exit code: 1")
                       && (!outputAvailable || msg.Contains("Detected memory leaks!")));
        }

        [TestMethod]
        public override void MemoryLeakTests_PassingWithLeaks_CorrectResult()
        {
            base.MemoryLeakTests_PassingWithLeaks_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_PassingWithoutLeaksRelease_CorrectResult()
        {
            base.MemoryLeakTests_PassingWithoutLeaksRelease_CorrectResult();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public override void MemoryLeakTests_PassingWithoutLeaks_CorrectResult()
        {
            RunMemoryLeakTest(TestResources.LeakCheckTests_DebugX86, "memory_leaks.passing", VsTestOutcome.Passed, VsTestOutcome.Passed,
                msg => msg == "");
        }

        [TestMethod]
        public override void MemoryLeakTests_FailingWithoutLeaks_CorrectResult()
        {
            base.MemoryLeakTests_FailingWithoutLeaks_CorrectResult();
        }

        [TestMethod]
        public override void MemoryLeakTests_ExitCodeTest_OnlyexitCodeTestResultAndNoWarnings()
        {
            base.MemoryLeakTests_ExitCodeTest_OnlyexitCodeTestResultAndNoWarnings();
        }

        #endregion
    }
}