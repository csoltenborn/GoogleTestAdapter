using System.IO;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
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
            string durationsFile = TestResources.SampleTests + GoogleTestConstants.DurationsExtension;
            if (File.Exists(durationsFile))
                File.Delete(durationsFile);
            File.Exists(durationsFile).Should().BeFalse();

            MockOptions.Setup(o => o.ParallelTestExecution).Returns(parallelExecution);
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);
            var processExecutor = new ProcessExecutor(null, TestEnvironment);

            var testExecutor = new GoogleTestExecutor(TestEnvironment);
            testExecutor.RunTests(TestDataCreator.AllTestCasesOfSampleTests, TestDataCreator.AllTestCasesOfSampleTests, MockFrameworkReporter.Object, null, false, TestResources.SampleTestsSolutionDir, processExecutor);

            File.Exists(durationsFile)
                .Should()
                .BeTrue($"Parallel execution should result in test durations file at location {durationsFile}");

            var durations = new TestDurationSerializer().ReadTestDurations(TestDataCreator.AllTestCasesOfSampleTests);
            durations.Keys.Should().Contain(TestDataCreator.AllTestCasesOfSampleTests);
        }

    }

}