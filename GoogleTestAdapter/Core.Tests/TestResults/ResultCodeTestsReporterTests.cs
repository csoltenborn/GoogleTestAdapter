using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.TestResults
{
    [TestClass]
    public class ResultCodeTestsReporterTests : TestsBase
    {
        private readonly Mock<IResultCodeTestsAggregator> _mockAggregator = new Mock<IResultCodeTestsAggregator>();
        private ResultCodeTestsReporter _reporter;

        private const string ResultCodeTestCaseName = nameof(ResultCodeTestCaseName);

        [TestInitialize]
        public override void SetUp()
        {
            base.SetUp();

            MockOptions.Setup(o => o.ReturnCodeTestCase).Returns(ResultCodeTestCaseName);

            _mockAggregator.Reset();
            _reporter = new ResultCodeTestsReporter(MockFrameworkReporter.Object, _mockAggregator.Object, MockOptions.Object, MockLogger.Object);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_EmptyInput_EmptyResult()
        {
            MockOptions.Setup(o => o.ReturnCodeTestCase).Returns("");
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo")
                });

            _reporter.ReportResultCodeTestCases(null, false);
            
            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.IsAny<IEnumerable<TestResult>>()), Times.Never);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_Pass_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo")
                });
            _reporter.ReportResultCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Passed))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_PassWithOutput_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", resultCodeOutput: new List<string> {"Output 1", "", "Output 2"})
                });
            _reporter.ReportResultCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Passed, "Output 1", "Output 2"))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_Fail_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1)
                });
            _reporter.ReportResultCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Failed))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_FailWithOutput_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1, resultCodeOutput: new List<string> {"Output 1", "", "Output 2"})
                });
            _reporter.ReportResultCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Failed, "Output 1", "Output 2"))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_PassAndSkip_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", resultCodeSkip: true)
                });

            _reporter.ReportResultCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Skipped))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_FailAndSkip_CorrectResult()
        {
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1, resultCodeSkip: true)
                });

            _reporter.ReportResultCodeTestCases(null, false);

            MockFrameworkReporter
                .Verify(r => r.ReportTestResults(It.Is<IEnumerable<TestResult>>(
                        results => CheckResult(results.Single(), "Foo", TestOutcome.Failed))), 
                    Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_PassButNoOutput_WarningIsLogged()
        {
            MockOptions.Setup(o => o.UseNewTestExecutionFramework).Returns(false);
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo")
                });

            _reporter.ReportResultCodeTestCases(null, true);

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(msg => msg.Contains("collected") && msg.Contains(SettingsWrapper.OptionUseNewTestExecutionFramework))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReportResultCodeTestCases_FailButNoOutput_WarningIsLogged()
        {
            MockOptions.Setup(o => o.UseNewTestExecutionFramework).Returns(false);
            _mockAggregator.Setup(a => a.ComputeAggregatedResults(It.IsAny<IEnumerable<ExecutableResult>>())).Returns(
                new List<ExecutableResult>
                {
                    new ExecutableResult("Foo", 1)
                });

            _reporter.ReportResultCodeTestCases(null, true);

            MockLogger.Verify(l => l.LogWarning(It.Is<string>(msg => msg.Contains("collected") && msg.Contains(SettingsWrapper.OptionUseNewTestExecutionFramework))), Times.Once);
        }

        private static bool CheckResult(TestResult result, string executable, TestOutcome outcome, params string[] errorMessageParts)
        {
            return
                result.TestCase.Source == executable &&
                result.DisplayName == $"{executable}.{ResultCodeTestCaseName}" &&
                result.TestCase.FullyQualifiedName == $"{executable}.{ResultCodeTestCaseName}" &&
                result.Outcome == outcome &&
                errorMessageParts.All(p => result.ErrorMessage.Contains(p));
        }
    }
}
