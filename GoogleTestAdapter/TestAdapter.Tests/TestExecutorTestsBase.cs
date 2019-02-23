using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.DiaResolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestResults;
using GoogleTestAdapter.Tests.Common;
using VsTestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using VsTestOutcome = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter
{
    public abstract class TestExecutorTestsBase : TestAdapterTestsBase
    {
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

        protected virtual void SetUpMockFrameworkHandle()
        {
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
            SetUpMockFrameworkHandle();
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
            SetUpMockFrameworkHandle();
            MockOptions.Setup(o => o.WorkingDir).Returns(SettingsWrapper.SolutionDirPlaceholder);

            RunAndVerifySingleTest(testCase, VsTestOutcome.Passed);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_ResultCodeTest_FailingTestResultIsProduced()
        {
            RunResultCodeTest("TestMath.AddFails", VsTestOutcome.Failed);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_ResultCodeTest_PassingTestResultIsProduced()
        {
            RunResultCodeTest("TestMath.AddPasses", VsTestOutcome.Passed);
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
            MockOptions.Setup(o => o.BatchForTestSetup).Returns($"$(SolutionDir){TestResources.SucceedingBatch}");
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns($"$(SolutionDir){TestResources.FailingBatch}");

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.AtLeastOnce());
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithSetupAndTeardownBatchesWhereSetupFails_LogsWarning()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns($"$(SolutionDir){TestResources.FailingBatch}");
            MockOptions.Setup(o => o.BatchForTestTeardown).Returns($"$(SolutionDir){TestResources.SucceedingBatch}");

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.AtLeastOnce());
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithoutBatches_NoLogging()
        {
            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0);

            MockLogger.Verify(l => l.LogInfo(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup))),
                Times.Never);
            MockLogger.Verify(l => l.LogInfo(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
            MockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestTeardown))),
                Times.Never);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void RunTests_WithNonexistingSetupBatch_LogsError()
        {
            MockOptions.Setup(o => o.BatchForTestSetup).Returns("some_nonexisting_file");

            RunAndVerifyTests(TestResources.DllTests_ReleaseX86, 1, 1, 0, checkNoErrorsLogged: false);

            MockLogger.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains(PreparingTestRunner.TestSetup.ToLower()))),
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

        protected void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfSkippedTests = 0, bool checkNoErrorsLogged = true)
        {
            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
            executor.RunTests(executable.Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            if (checkNoErrorsLogged)
            {
                MockLogger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
                MockLogger.Verify(l => l.DebugError(It.IsAny<string>()), Times.Never);
            }

            CheckMockInvocations(nrOfPassedTests, nrOfFailedTests, nrOfUnexecutedTests, nrOfSkippedTests);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void MemoryLeakTests_PassingWithLeaks_CorrectResult()
        {
            bool outputAvailable = MockOptions.Object.UseNewTestExecutionFramework ||
                                   !MockRunContext.Object.IsBeingDebugged;
            RunMemoryLeakTest(TestResources.LeakCheckTests_DebugX86, "memory_leaks.passing_and_leaking", VsTestOutcome.Passed, VsTestOutcome.Failed,
                msg => msg.Contains("Exit code: 1")
                       && (!outputAvailable || msg.Contains("Detected memory leaks!")));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void MemoryLeakTests_FailingWithLeaks_CorrectResult()
        {
            bool outputAvailable = MockOptions.Object.UseNewTestExecutionFramework ||
                                   !MockRunContext.Object.IsBeingDebugged;
            RunMemoryLeakTest(TestResources.LeakCheckTests_DebugX86, "memory_leaks.failing_and_leaking", VsTestOutcome.Failed, VsTestOutcome.Failed,
                msg => msg.Contains("Exit code: 1")
                          && (!outputAvailable || msg.Contains("Detected memory leaks!")));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void MemoryLeakTests_PassingWithoutLeaks_CorrectResult()
        {
            RunMemoryLeakTest(TestResources.LeakCheckTests_DebugX86, "memory_leaks.passing", VsTestOutcome.Passed, VsTestOutcome.Passed,
                msg => msg == null);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void MemoryLeakTests_FailingWithoutLeaks_CorrectResult()
        {
            try
            {
                RunMemoryLeakTest(TestResources.LeakCheckTests_DebugX86, "memory_leaks.failing", VsTestOutcome.Failed, VsTestOutcome.Passed,
                    msg => msg == null);
            }
            catch (MockException)
            {
                Assert.Inconclusive("skipped until gtest's 'memory leaks' are fixed...");
            }
            Assert.Fail("Memory leak problem has been fixed :-) - enable test!");
        }

        [TestMethod]
        [TestCategory(Integration)]
        public virtual void MemoryLeakTests_PassingWithoutLeaksRelease_CorrectResult()
        {
            bool outputAvailable = MockOptions.Object.UseNewTestExecutionFramework ||
                                   !MockRunContext.Object.IsBeingDebugged;
            RunMemoryLeakTest(TestResources.LeakCheckTests_ReleaseX86, "memory_leaks.passing_and_leaking", VsTestOutcome.Passed, 
                outputAvailable ? VsTestOutcome.Skipped : VsTestOutcome.Passed,
                msg => !outputAvailable || msg.Contains("Memory leak detection is only performed if compiled with Debug configuration."));
        }

        private void RunMemoryLeakTest(string executable, string testCaseName, VsTestOutcome testOutcome, VsTestOutcome leakCheckOutcome, Func<string, bool> errorMessagePredicate)
        {
            string resultCodeTestName = "MemoryLeakTest";
            MockOptions.Setup(o => o.ReturnCodeTestCase).Returns(resultCodeTestName);
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-is_run_by_gta");

            var testCases = new GoogleTestDiscoverer(MockLogger.Object, TestEnvironment.Options, 
                new ProcessExecutorFactory(), new DefaultDiaResolverFactory()).GetTestsFromExecutable(executable);
            var testCase = testCases.Single(tc => tc.DisplayName == testCaseName);

            var executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
            executor.RunTests(testCase.Yield().Select(tc => tc.ToVsTestCase()), MockRunContext.Object, MockFrameworkHandle.Object);

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(result =>
                    result.TestCase.FullyQualifiedName == testCaseName
                    && result.Outcome == testOutcome
                )),
                Times.Once);           
            
            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(result =>
                    result.TestCase.FullyQualifiedName.EndsWith("MemoryLeakTest")
                    && result.Outcome == leakCheckOutcome
                    && (result.ErrorMessage == null || !result.ErrorMessage.Contains(StreamingStandardOutputTestResultParser.GtaResultCodeOutputBegin))
                    && (result.ErrorMessage == null || !result.ErrorMessage.Contains(StreamingStandardOutputTestResultParser.GtaResultCodeOutputEnd))
                    && errorMessagePredicate(result.ErrorMessage)
                )),
                Times.Once);
        }

        private void RunResultCodeTest(string testCaseName, VsTestOutcome outcome)
        {
            string resultCodeTestName = "ResultCode";
            MockOptions.Setup(o => o.ReturnCodeTestCase).Returns(resultCodeTestName);

            TestCase testCase = TestDataCreator.GetTestCases(testCaseName).First();

            TestExecutor executor = new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options);
            executor.RunTests(testCase.ToVsTestCase().Yield(), MockRunContext.Object, MockFrameworkHandle.Object);

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(result =>
                    result.TestCase.FullyQualifiedName == testCaseName
                    && result.Outcome == outcome
                )),
                Times.Once);

            // ReSharper disable once PossibleNullReferenceException
            string finalName = Path.GetFileName(testCase.Source).Replace(".", "_") + "." + resultCodeTestName;
            bool outputAvailable = MockOptions.Object.UseNewTestExecutionFramework ||
                                   !MockRunContext.Object.IsBeingDebugged;
            Func<VsTestResult, bool> errorMessagePredicate = outcome == VsTestOutcome.Failed
                ? result => result.ErrorMessage.Contains("Exit code: 1")
                            && (!outputAvailable || result.ErrorMessage.Contains("The result code output"))
                            && !result.ErrorMessage.Contains(StreamingStandardOutputTestResultParser.GtaResultCodeOutputBegin)
                            && !result.ErrorMessage.Contains(StreamingStandardOutputTestResultParser.GtaResultCodeOutputEnd)
                            && !result.ErrorMessage.Contains("Some more output")
                : (Func<VsTestResult, bool>) (result => result.ErrorMessage == null || result.ErrorMessage.Contains("The result code output"));
            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<VsTestResult>(result =>
                    result.TestCase.FullyQualifiedName == finalName
                    && result.Outcome == outcome
                    && errorMessagePredicate(result)
                )),
                Times.Once);

            MockFrameworkHandle.Verify(h => h.RecordResult(It.IsAny<VsTestResult>()), Times.Exactly(2));

            if (!outputAvailable && outcome == VsTestOutcome.Failed)
            {
                MockLogger.Verify(l => l.LogWarning(It.Is<string>(msg => msg.Contains("Result code") 
                                                                           && msg.Contains(SettingsWrapper.OptionUseNewTestExecutionFramework))), Times.Once);
            }
        }

    }

}