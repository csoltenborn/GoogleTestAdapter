using System;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using VsTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestAdapter.Framework
{
    [TestClass]
    public class VsTestFrameworkReporterTests : TestAdapterTestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportTestResults_InVisualStudio_ErrorMessageStartsWithNewline()
        {
            DoTestBeginOfErrorMessage(true, Environment.NewLine);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportTestResults_FromVsTestConsole_ErrorMessageStartsInSameline()
        {
            DoTestBeginOfErrorMessage(false, "");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportTestResults_FromVsTestConsole_StacktraceEndsWithoutNewline()
        {
            var errorStacktrace = "My stack trace";
            var result = new TestResult(TestDataCreator.ToTestCase("MyTestCase"))
            {
                Outcome = TestOutcome.Failed,
                ErrorStackTrace = errorStacktrace + Environment.NewLine,
                ComputerName = "My Computer"
            };

            var reporter = new VsTestFrameworkReporter(MockFrameworkHandle.Object, false, MockLogger.Object);
            reporter.ReportTestResults(result.Yield());

            MockFrameworkHandle.Verify(h => h.RecordResult(
                It.Is<VsTestResult>(tr => tr.ErrorStackTrace.Equals(errorStacktrace))),
                Times.Exactly(1));
        }

        private void DoTestBeginOfErrorMessage(bool inVisualStudio, string beginOfErrorMessage)
        {
            var errorMessage = "My error message";
            var result = new TestResult(TestDataCreator.ToTestCase("MyTestCase"))
            {
                Outcome = TestOutcome.Failed,
                ErrorMessage = errorMessage,
                ComputerName = "My Computer"
            };

            var reporter = new VsTestFrameworkReporter(MockFrameworkHandle.Object, inVisualStudio, MockLogger.Object);
            reporter.ReportTestResults(result.Yield());

            MockFrameworkHandle.Verify(h => h.RecordResult(
                It.Is<VsTestResult>(tr => tr.ErrorMessage.Equals(beginOfErrorMessage + errorMessage))),
                Times.Exactly(1));
        }

    }

}