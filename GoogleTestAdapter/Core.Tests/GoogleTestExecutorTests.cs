using System.IO;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Scheduling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestExecutorTests : AbstractCoreTests
    {

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_SequentialExecution_ProducesTestDurationsFile()
        {
            AssertDurationsFileIsCreated(false);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void RunTests_ParallelExecution_ProducesTestDurationsFile()
        {
            AssertDurationsFileIsCreated(true);
        }

        private void AssertDurationsFileIsCreated(bool parallelExecution)
        {
            string sampleTestsDurationsFile = TestResources.SampleTests + GoogleTestConstants.DurationsExtension;
            RemoveFileIfNecessary(sampleTestsDurationsFile);

            string crashingTestsDurationsFile = TestResources.HardCrashingSampleTests + GoogleTestConstants.DurationsExtension;
            RemoveFileIfNecessary(crashingTestsDurationsFile);

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(parallelExecution);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);
            var processExecutor = new ProcessExecutor(null, TestEnvironment);

            var collectingReporter = new FakeFrameworkReporter();
            var testExecutor = new GoogleTestExecutor(TestEnvironment);
            testExecutor.RunTests(TestDataCreator.AllTestCasesExceptLoadTests, TestDataCreator.AllTestCasesExceptLoadTests, collectingReporter, null, false, TestResources.SampleTestsSolutionDir, processExecutor);

            File.Exists(sampleTestsDurationsFile)
                .Should()
                .BeTrue($"Test execution should result in test durations file at location {sampleTestsDurationsFile}");
            var durations = new TestDurationSerializer().ReadTestDurations(TestDataCreator.AllTestCasesOfSampleTests);
            durations.Keys.Should().Contain(
                TestDataCreator.AllTestCasesOfSampleTests.Where(tc => collectingReporter.ReportedTestResults.Any(tr => tc.Equals(tr.TestCase) && (tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))));

            File.Exists(crashingTestsDurationsFile)
                .Should()
                .BeTrue($"Test execution should result in test durations file at location {crashingTestsDurationsFile}");
            durations = new TestDurationSerializer().ReadTestDurations(TestDataCreator.AllTestCasesOfHardCrashingTests);
            durations.Keys.Should().Contain(TestDataCreator.AllTestCasesOfHardCrashingTests.Where(tc => collectingReporter.ReportedTestResults.Any(tr => tc.Equals(tr.TestCase) && (tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))));
        }

        private void RemoveFileIfNecessary(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
            File.Exists(file).Should().BeFalse();
        }

    }

}