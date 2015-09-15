using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class TestDurationSerializerTests
    {

        [TestMethod]
        public void TestDurationIsWrittenAndReadCorrectly()
        {
            List<TestResult> testResults = new List<TestResult>();
            string tempFile = Path.GetTempFileName();
            testResults.Add(new TestResult(new TestCase("TestSuite1.Test1", new Uri("http://nothing"), tempFile))
            {
                Duration = TimeSpan.FromMilliseconds(3),
                Outcome = TestOutcome.Passed
            });
            testResults.Add(new TestResult(new TestCase("TestSuite1.SkippedTest", new Uri("http://nothing"), tempFile))
            {
                Duration = TimeSpan.FromMilliseconds(1),
                Outcome = TestOutcome.Skipped
            });

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            string durationsFile = GetDurationsFile(serializer, tempFile);
            Assert.IsTrue(File.Exists(durationsFile));

            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(TR => TR.TestCase));
            Assert.AreEqual(1, durations.Count);
            Assert.IsTrue(durations.ContainsKey(testResults[0].TestCase));
            Assert.AreEqual(3, durations[testResults[0].TestCase]);
            Assert.IsFalse(durations.ContainsKey(testResults[1].TestCase));

            File.Delete(durationsFile);
        }

        [TestMethod]
        public void SameTestsInDifferentExecutables()
        {
            List<TestResult> testResults = new List<TestResult>();
            string tempFile = Path.GetTempFileName();
            testResults.Add(new TestResult(new TestCase("TestSuite1.Test1", new Uri("http://nothing"), tempFile))
            {
                Duration = TimeSpan.FromMilliseconds(3),
                Outcome = TestOutcome.Passed
            });
            string tempFile2 = Path.GetTempFileName();
            testResults.Add(new TestResult(new TestCase("TestSuite1.Test1", new Uri("http://nothing"), tempFile2))
            {
                Duration = TimeSpan.FromMilliseconds(4),
                Outcome = TestOutcome.Failed
            });

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);

            string durationsFile1 = GetDurationsFile(serializer, tempFile);
            Assert.IsTrue(File.Exists(durationsFile1));
            string durationsFile2 = GetDurationsFile(serializer, tempFile2);
            Assert.IsTrue(File.Exists(durationsFile2));

            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(TR => TR.TestCase));
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
            List<TestResult> testResults = new List<TestResult>();
            string tempFile = Path.GetTempFileName();
            testResults.Add(new TestResult(new TestCase("TestSuite1.Test1", new Uri("http://nothing"), tempFile))
            {
                Duration = TimeSpan.FromMilliseconds(3),
                Outcome = TestOutcome.Passed
            });

            TestDurationSerializer serializer = new TestDurationSerializer();
            serializer.UpdateTestDurations(testResults);
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(testResults.Select(TR => TR.TestCase));
            Assert.AreEqual(3, durations[testResults[0].TestCase]);

            testResults[0].Duration = TimeSpan.FromMilliseconds(4);
            serializer.UpdateTestDurations(testResults);
            durations = serializer.ReadTestDurations(testResults.Select(TR => TR.TestCase));
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
            IDictionary<TestCase, int> durations = serializer.ReadTestDurations(new TestCase("TestSuite1.Test1", new Uri("http://nothing"), tempFile).Yield());

            Assert.IsNotNull(durations);
            Assert.AreEqual(0, durations.Count);
        }

        [TestMethod]
        public void DurationFileWithoutCurrentTestResultsInEmptyDictionary()
        {
            List<TestResult> testResults = new List<TestResult>();
            string tempFile = Path.GetTempFileName();
            testResults.Add(new TestResult(new TestCase("TestSuite1.Test1", new Uri("http://nothing"), tempFile))
            {
                Duration = TimeSpan.FromMilliseconds(3)
            });

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