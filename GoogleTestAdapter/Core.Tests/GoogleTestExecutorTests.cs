using System.IO;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.TestResults;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestExecutorTests : TestsBase
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
            string sampleTestsDurationsFile = TestResources.Tests_DebugX86 + GoogleTestConstants.DurationsExtension;
            RemoveFileIfNecessary(sampleTestsDurationsFile);

            string crashingTestsDurationsFile = TestResources.CrashingTests_DebugX86 + GoogleTestConstants.DurationsExtension;
            RemoveFileIfNecessary(crashingTestsDurationsFile);

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(parallelExecution);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);
            MockOptions.Setup(o => o.SolutionDir).Returns(TestResources.SampleTestsSolutionDir);

            var collectingReporter = new FakeFrameworkReporter();
            var exitCodeTestsAggregator = new ExitCodeTestsAggregator();
            var exitCodeTestsReporter = new ExitCodeTestsReporter(MockFrameworkReporter.Object, exitCodeTestsAggregator, MockOptions.Object, MockLogger.Object);
            var testExecutor = new GoogleTestExecutor(TestEnvironment.Logger, TestEnvironment.Options, ProcessExecutorFactory, exitCodeTestsReporter);
            testExecutor.RunTests(TestDataCreator.AllTestCasesExceptLoadTests, collectingReporter,false);

            sampleTestsDurationsFile.AsFileInfo()
                .Should()
                .Exist("Test execution should result in test durations");
            var durations = new TestDurationSerializer().ReadTestDurations(TestDataCreator.AllTestCasesOfSampleTests);
            durations.Keys.Should().Contain(
                TestDataCreator.AllTestCasesOfSampleTests.Where(tc => collectingReporter.ReportedTestResults.Any(tr => tc.Equals(tr.TestCase) && (tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))));

            crashingTestsDurationsFile.AsFileInfo()
                .Should()
                .Exist("Test execution should result in test durations file");
            durations = new TestDurationSerializer().ReadTestDurations(TestDataCreator.AllTestCasesOfHardCrashingTests);
            durations.Keys.Should().Contain(TestDataCreator.AllTestCasesOfHardCrashingTests.Where(tc => collectingReporter.ReportedTestResults.Any(tr => tc.Equals(tr.TestCase) && (tr.Outcome == TestOutcome.Passed || tr.Outcome == TestOutcome.Failed))));
        }

        private void RemoveFileIfNecessary(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
            file.AsFileInfo().Should().NotExist("it was just deleted");
        }

    }

}