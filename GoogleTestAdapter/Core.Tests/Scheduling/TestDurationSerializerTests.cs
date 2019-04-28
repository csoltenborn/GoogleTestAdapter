using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Tests.Common;
using GoogleTestAdapter.Tests.Common.Assertions;
using GoogleTestAdapter.Tests.Common.Helpers;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class TestDurationSerializerTests : TestsBase
    {

        [TestMethod]
        [TestCategory(Unit)]
        public void UpdateTestDurations_SimpleTests_DurationsAreWrittenAndReadCorrectly()
        {
            string tempFile = Path.GetTempFileName();
            List<Model.TestResult> testResults = new List<Model.TestResult>
            {
                TestDataCreator.ToTestResult("TestSuite1.Test1", Model.TestOutcome.Passed, 3, tempFile),
                TestDataCreator.ToTestResult("TestSuite1.SkippedTest", Model.TestOutcome.Skipped, 1, tempFile)
            };

            var serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            string durationsFile = GetDurationsFile(serializer, tempFile);
            durationsFile.AsFileInfo().Should().Exist();

            IDictionary<Model.TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            durations.Should().HaveCount(1);
            durations.Should().ContainKey(testResults[0].TestCase);
            durations[testResults[0].TestCase].Should().Be(3);
            durations.Should().NotContainKey(testResults[1].TestCase);

            File.Delete(durationsFile);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void UpdateTestDurations_SameTestsInDifferentExecutables_DurationsAreWrittenAndReadCorrectly()
        {
            string tempFile = Path.GetTempFileName();
            string tempFile2 = Path.GetTempFileName();
            List<Model.TestResult> testResults = new List<Model.TestResult>
            {
                TestDataCreator.ToTestResult("TestSuite1.Test1", Model.TestOutcome.Passed, 3, tempFile),
                TestDataCreator.ToTestResult("TestSuite1.Test1", Model.TestOutcome.Failed, 4, tempFile2)
            };

            var serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            string durationsFile1 = GetDurationsFile(serializer, tempFile);
            durationsFile1.AsFileInfo().Should().Exist();
            string durationsFile2 = GetDurationsFile(serializer, tempFile2);
            durationsFile2.AsFileInfo().Should().Exist();

            IDictionary<Model.TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            durations.Should().HaveCount(2);
            durations.Should().ContainKey(testResults[0].TestCase);
            durations[testResults[0].TestCase].Should().Be(3);
            durations.Should().ContainKey(testResults[1].TestCase);
            durations[testResults[1].TestCase].Should().Be(4);

            File.Delete(durationsFile1);
            File.Delete(durationsFile2);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void UpdateTestDurations_SingleTest_DurationIsUpdatedCorrectly()
        {
            string tempFile = Path.GetTempFileName();
            List<Model.TestResult> testResults = new List<Model.TestResult>
            {
                TestDataCreator.ToTestResult("TestSuite1.Test1", Model.TestOutcome.Passed, 3, tempFile)
            };

            var serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);
            IDictionary<Model.TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            durations[testResults[0].TestCase].Should().Be(3);

            testResults[0].Duration = TimeSpan.FromMilliseconds(4);
            serializer.UpdateTestDurations(testResults);
            durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            durations.Should().HaveCount(1);
            durations.Should().ContainKey(testResults[0].TestCase);
            durations[testResults[0].TestCase].Should().Be(4);

            File.Delete(GetDurationsFile(serializer, tempFile));
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReadTestDurations_NoDurationFile_EmptyDictionary()
        {
            string tempFile = Path.GetTempFileName();

            var serializer = new TestDurationSerializer();
            IDictionary<Model.TestCase, int> durations = serializer.ReadTestDurations(TestDataCreator.ToTestCase("TestSuite1.Test1", tempFile).Yield());

            durations.Should().NotBeNull();
            durations.Should().BeEmpty();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void ReadTestDurations_DurationFileWithoutCurrentTest_EmptyDictionary()
        {
            string tempFile = Path.GetTempFileName();
            List<Model.TestResult> testResults = new List<Model.TestResult>
            {
                TestDataCreator.ToTestResult("TestSuite1.Test1", Model.TestOutcome.None, 3, tempFile)
            };

            var serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            IDictionary<Model.TestCase, int> durations = serializer.ReadTestDurations(
                new Model.TestCase("TestSuite1.Test2", tempFile, "TestSuite1.Test2", "", 0).Yield());

            durations.Should().NotBeNull();
            durations.Should().BeEmpty();

            File.Delete(GetDurationsFile(serializer, tempFile));
        }


        private string GetDurationsFile(TestDurationSerializer serializer, string executable)
        {
            PrivateObject serializerAccessor = new PrivateObject(serializer);
            return serializerAccessor.Invoke("GetDurationsFile", executable) as string;
        }

    }

}