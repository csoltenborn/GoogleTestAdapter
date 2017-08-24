// This file has been modified by Microsoft on 8/2017.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using VsTestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    public abstract class TestExecutorTestsBase : TestAdapterTestsBase
    {
        private const string TestSetup = "Test setup";
        private const string TestTeardown = "Test teardown";
        private readonly bool _parallelTestExecution;
        private readonly int _maxNrOfThreads;


        protected TestExecutorTestsBase(bool parallelTestExecution, int maxNrOfThreads)
        {
            _parallelTestExecution = parallelTestExecution;
            _maxNrOfThreads = maxNrOfThreads;
        }


        protected virtual void CheckMockInvocations(int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfSkippedTests)
        {
            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(tr => tr.Outcome == VsTestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<VsTestCase>(), It.Is<VsTestOutcome>(to => to == VsTestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
        }

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(_parallelTestExecution);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(_maxNrOfThreads);
        }

        private void RunAndVerifySingleTest(TestCase testCase, VsTestOutcome expectedOutcome)
        {
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
            executor.RunTests(testCase.ToVsTestCase().Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            foreach (VsTestOutcome outcome in Enum.GetValues(typeof(VsTestOutcome)))
            {
                MockFrameworkHandle.Verify(h => h.RecordEnd(It.IsAny<VsTestCase>(), It.Is<VsTestOutcome>(to => to == outcome)),
                    Times.Exactly(outcome == expectedOutcome ? 1 : 0));
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_TestDirectoryViaUserParams_IsPassedViaCommandLineArg()
        {
            TestCase testCase = TestDataCreator.GetTestCases("CommandArgs.TestDirectoryIsSet").First();

            RunAndVerifySingleTest(testCase, VsTestOutcome.Failed);

            MockFrameworkHandle.Reset();
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-testdirectory=\"" + SettingsWrapper.TestDirPlaceholder + "\"");

            RunAndVerifySingleTest(testCase, VsTestOutcome.Passed);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WorkingDir_IsSetCorrectly()
        {
            TestCase testCase = TestDataCreator.GetTestCases("WorkingDir.IsSolutionDirectory").First();

            MockOptions.Setup(o => o.WorkingDir).Returns(SettingsWrapper.ExecutableDirPlaceholder);
            RunAndVerifySingleTest(testCase, VsTestOutcome.Failed);

            MockFrameworkHandle.Reset();
            MockOptions.Setup(o => o.WorkingDir).Returns(SettingsWrapper.SolutionDirPlaceholder);
            RunAndVerifySingleTest(testCase, VsTestOutcome.Passed);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_ExternallyLinkedX86Tests_CorrectTestResults()
        {
            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_ExternallyLinkedX86TestsInDebugMode_CorrectTestResults()
        {
            // for at least having the debug messaging code executed once
            MockOptions.Setup(o => o.DebugMode).Returns(true);

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_StaticallyLinkedX86Tests_CorrectTestResults()
        {
            // let's print the test output
            MockOptions.Setup(o => o.PrintTestOutput).Returns(true);

            RunAndVerifyTests(TestResources.Tests_DebugX86, TestResources.NrOfPassingTests, TestResources.NrOfFailingTests, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_ExternallyLinkedX64_CorrectTestResults()
        {
            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_StaticallyLinkedX64Tests_CorrectTestResults()
        {
            RunAndVerifyTests(TestResources.Tests_ReleaseX64, TestResources.NrOfPassingTests, TestResources.NrOfFailingTests, 0);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_HardCrashingX86Tests_CorrectTestResults()
        {
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
            executor.RunTests(TestResources.CrashingTests_DebugX86.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(1, 2, 0, 3);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithSetupAndTeardownBatchesWhereTeardownFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(TestResources.SucceedingBatch);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(TestResources.FailingBatch);

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(TestTeardown))),
                Times.AtLeastOnce());
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns(TestResources.FailingBatch);
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns(TestResources.SucceedingBatch);

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(TestSetup))),
                Times.AtLeastOnce());
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(TestTeardown))),
                Times.Never);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithoutBatches_NoLogging()
        {
            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogInfo(
                It.Is<string>(s => s.Contains(TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogInfo(
                It.Is<string>(s => s.Contains(TestTeardown))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(TestTeardown))),
                Times.Never);
            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(TestTeardown))),
                Times.Never);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithNonexistingSetupBatch_LogsError()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns("some_nonexisting_file");

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(TestSetup))),
                Times.AtLeastOnce());
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithPathExtension_ExecutionOk()
        {
            string baseDir = TestDataCreator.PreparePathExtensionTest();
            try
            {
                string targetExe = TestDataCreator.GetPathExtensionExecutable(baseDir);
                MockOptions.Setup(o => o.PathExtension).Returns(SettingsWrapper.ExecutableDirPlaceholder + @"\..\dll");

                var executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
                executor.RunTests(targetExe.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

                MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(tr => tr.Outcome == VsTestOutcome.Passed)), Times.Once);
                MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(tr => tr.Outcome == VsTestOutcome.Failed)), Times.Once);
                MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                Utils.DeleteDirectory(baseDir).Should().BeTrue();
            }
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithoutPathExtension_ExecutionFails()
        {
            string baseDir = TestDataCreator.PreparePathExtensionTest();
            try
            {
                string targetExe = TestDataCreator.GetPathExtensionExecutable(baseDir);

                var executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
                executor.RunTests(targetExe.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

                MockFrameworkHandle.Verify(h => h.RecordResult(It.IsAny<VsTestResult>()), Times.Never);
                MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Once);
            }
            finally
            {
                Utils.DeleteDirectory(baseDir).Should().BeTrue();
            }
        }

        protected void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfSkippedTests = 0)
        {
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
            executor.RunTests(executable.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfSkippedTests);
        }

    }

}