using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.ProcessExecution;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class ExitCodeTestsReporterTests : TestsBase
    {
        private readonly Mock<IExitCodeTestsAggregator> _mockAggregator = new Mock<IExitCodeTestsAggregator>();
        private ExitCodeTestsReporter _reporter;

        private const string ExitCodeTestCaseName = nameof(ExitCodeTestCaseName);

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockOptions.Setup(o => o.ExitCodeTestCase).Returns(ExitCodeTestCaseName);

            _mockAggregator.Reset();
            _reporter = new ExitCodeTestsReporter(MockFrameworkReporter.Object, _mockAggregator.Object, MockOptions.Object, MockLogger.Object);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_EmptyInput_EmptyResult()
        {
            MockOptions.Setup(o => o.ExitCodeTestCase).Returns("");
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo")
                });

            _reporter.ReportExitCodeTestCases(null, false);
            
            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.IsAny<IEnumerable<TestResult>>()), Times.Never);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_Pass_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo")
                });
            _reporter.ReportExitCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Passed))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_PassWithOutput_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", exitCodeOutput: new List<string> {"Output 1", "", "Output 2"})
                });
            _reporter.ReportExitCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Passed, "Output 1", "Output 2"))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_Fail_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1)
                });
            _reporter.ReportExitCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Failed))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_FailWithOutput_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1, exitCodeOutput: new List<string> {"Output 1", "", "Output 2"})
                });
            _reporter.ReportExitCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Failed, "Output 1", "Output 2"))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_PassAndSkip_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", exitCodeSkip: true)
                });

            _reporter.ReportExitCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Skipped))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_FailAndSkip_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1, exitCodeSkip: true)
                });

            _reporter.ReportExitCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Skipped))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_PassButNoOutput_WarningIsLogged()
        {
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo")
                });

            _reporter.ReportExitCodeTestCases(null, true);

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(msg => msg.Contains("collected") && msg.Contains(SettingsWrapper.OptionDebuggerKind))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportExitCodeTestCases_FailButNoOutput_WarningIsLogged()
        {
            MockOptions.Setup(o => o.DebuggerKind).Returns(DebuggerKind.VsTestFramework);
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1)
                });

            _reporter.ReportExitCodeTestCases(null, true);

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(msg => msg.Contains("collected") && msg.Contains(SettingsWrapper.OptionDebuggerKind))), Times.Once);
        }

        private static bool CheckResult(TestResult result, string executable, TestOutcome outcome, params string[] errorMessageParts)
        {
            return
                result.TestCase.Source == executable &&
                result.DisplayName == $"{ExitCodeTestCaseName}.{executable}" &&
                result.TestCase.FullyQualifiedName == $"{ExitCodeTestCaseName}.{executable}" &&
                result.Outcome == outcome &&
                errorMessageParts.All(p => result.ErrorMessage.Contains(p));
        }
    }
}
