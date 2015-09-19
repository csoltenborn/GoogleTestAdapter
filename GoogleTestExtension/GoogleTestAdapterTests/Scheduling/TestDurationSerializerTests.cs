using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using GoogleTestAdapter.Helpers;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class TestDurationSerializerTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void TestDurationIsWrittenAndReadCorrectly()
        {
            string tempFile = Path.GetTempFileName();
            List<TestResult> testResults = new List<TestResult>
            {
                ToTestResult("TestSuite1.Test1", TestOutcome.Passed, 3, tempFile),
                ToTestResult("TestSuite1.SkippedTest", TestOutcome.Skipped, 1, tempFile)
            };

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            string durationsFile = GetDurationsFile(serializer, tempFile);
            Assert.IsTrue(File.Exists(durationsFile));

            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            Assert.AreEqual(1, durations.Count);
            Assert.IsTrue(durations.ContainsKey(testResults[0].TestCase));
            Assert.AreEqual(3, durations[testResults[0].TestCase]);
            Assert.IsFalse(durations.ContainsKey(testResults[1].TestCase));

            File.Delete(durationsFile);
        }

        [TestMethod]
        public void SameTestsInDifferentExecutables()
        {
            string tempFile = Path.GetTempFileName();
            string tempFile2 = Path.GetTempFileName();
            List<TestResult> testResults = new List<TestResult>
            {
                ToTestResult("TestSuite1.Test1", TestOutcome.Passed, 3, tempFile),
                ToTestResult("TestSuite1.Test1", TestOutcome.Failed, 4, tempFile2)
            };

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            string durationsFile1 = GetDurationsFile(serializer, tempFile);
            Assert.IsTrue(File.Exists(durationsFile1));
            string durationsFile2 = GetDurationsFile(serializer, tempFile2);
            Assert.IsTrue(File.Exists(durationsFile2));

            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            Assert.AreEqual(2, durations.Count);
            Assert.IsTrue(durations.ContainsKey(testResults[0].TestCase));
            Assert.AreEqual(3, durations[testResults[0].TestCase]);
            Assert.IsTrue(durations.ContainsKey(testResults[1].TestCase));
            Assert.AreEqual(4, durations[testResults[1].TestCase]);

            File.Delete(durationsFile1);
            File.Delete(durationsFile2);
        }

        [TestMethod]
        public void TestDurationIsUpdatedCorrectly()
        {
            string tempFile = Path.GetTempFileName();
            List<TestResult> testResults = new List<TestResult>
            {
                ToTestResult("TestSuite1.Test1", TestOutcome.Passed, 3, tempFile)
            };

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            Assert.AreEqual(3, durations[testResults[0].TestCase]);

            testResults[0].Duration = TimeSpan.FromMilliseconds(4);
            serializer.UpdateTestDurations(testResults);
            durations = serializer.ReadTestDurations(testResults.Select(tr => tr.TestCase));
            Assert.AreEqual(1, durations.Count);
            Assert.IsTrue(durations.ContainsKey(testResults[0].TestCase));
            Assert.AreEqual(4, durations[testResults[0].TestCase]);

            File.Delete(GetDurationsFile(serializer, tempFile));
        }

        [TestMethod]
        public void NoDurationFileResultsInEmptyDictionary()
        {
            string tempFile = Path.GetTempFileName();

            TestDurationSerializer serializer = new TestDurationSerializer();
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(ToTestCase("TestSuite1.Test1", tempFile).Yield());

            Assert.IsNotNull(durations);
            Assert.AreEqual(0, durations.Count);
        }

        [TestMethod]
        public void DurationFileWithoutCurrentTestResultsInEmptyDictionary()
        {
            string tempFile = Path.GetTempFileName();
            List<TestResult> testResults = new List<TestResult>
            {
                ToTestResult("TestSuite1.Test1", TestOutcome.None, 3, tempFile)
            };

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(new TestCase("TestSuite1.Test2", new Uri("http://nothing"), tempFile).Yield());
            Assert.IsNotNull(durations);
            Assert.AreEqual(0, durations.Count);

            File.Delete(GetDurationsFile(serializer, tempFile));
        }

        private string GetDurationsFile(TestDurationSerializer serializer, string executable)
        {
            PrivateObject serializerAccessor = new PrivateObject(serializer);
            return serializerAccessor.Invoke("GetDurationsFile", executable) as string;
        }

    }

}