using System.Linq;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.TestAdapter.ProcessExecution;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;


namespace GoogleTestAdapter.TestAdapter.Settings
{
     [TestClass]
    public class HelperFileTests : TestAdapterTestsBase
    {
        private readonly Mock<IDebuggerAttacher> _mockDebuggerAttacher = new Mock<IDebuggerAttacher>();

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            _mockDebuggerAttacher.Reset();
            _mockDebuggerAttacher.Setup(a => a.AttachDebugger(It.IsAny<int>(), It.IsAny<DebuggerEngine>())).Returns(true);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void HelperFileTests_AdditionalParamsAreNotProvided_TestFails()
        {
            RunHelperFileTestsExecutable();

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => 
                tr.DisplayName.Contains("HelperFileTests.TheTargetIsSet") && 
                tr.Outcome == TestOutcome.Failed)), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void HelperFileTests_AdditionalParamsAreProvided_TestSucceeds()
        {
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-TheTarget=$(TheTarget)");

            RunHelperFileTestsExecutable();

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => 
                tr.DisplayName.Contains("HelperFileTests.TheTargetIsSet") && 
                tr.Outcome == TestOutcome.Passed)), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void HelperFileTests_WorkingDirIsSetFromProjectSettings_TestSucceeds()
        {
            MockOptions.Setup(o => o.AdditionalTestExecutionParam).Returns("-TheTarget=$(TheTarget)");
            MockOptions.Setup(o => o.WorkingDir).Returns("$(TheWorkingDirectory)");

            RunHelperFileTestsExecutable();

            MockFrameworkHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => 
                tr.DisplayName.Contains("HelperFileTests.TheTargetIsSet") && 
                tr.Outcome == TestOutcome.Passed)), Times.Once);
        }

        private void RunHelperFileTestsExecutable()
        {
            var testCase = new GoogleTestDiscoverer(MockLogger.Object, TestEnvironment.Options, new ProcessExecutorFactory())
                .GetTestsFromExecutable(TestResources.HelperFilesTests_ReleaseX86).Single();
            var executor =
                new TestExecutor(TestEnvironment.Logger, TestEnvironment.Options, _mockDebuggerAttacher.Object);
            executor.RunTests(testCase.ToVsTestCase().Yield(), MockRunContext.Object, MockFrameworkHandle.Object);
        }
    }
}