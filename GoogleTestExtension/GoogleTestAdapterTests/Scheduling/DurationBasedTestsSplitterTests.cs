using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.Linq;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class DurationBasedTestsSplitterTests : AbstractGoogleTestExtensionTests
    {
        [TestMethod]
        public void SimpleCase_TestsAreDistributedCorrectly()
        {
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            durations.Add(ToTestCase("ShortTest1"), 1);
            durations.Add(ToTestCase("ShortTest2"), 1);
            durations.Add(ToTestCase("LongTest"), 3);
            durations.Add(ToTestCase("ShortTest3"), 1);

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual("LongTest", result[0][0].FullyQualifiedName);
            Assert.AreEqual(3, result[1].Count);
        }

        [TestMethod]
        public void SimpleCaseWithThreeThreads_TestsAreDistributedCorrectly()
        {
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            durations.Add(ToTestCase("ShortTest1"), 1);
            durations.Add(ToTestCase("ShortTest2"), 1);
            durations.Add(ToTestCase("LongTest"), 3);
            durations.Add(ToTestCase("ShortTest3"), 1);

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(3);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual("LongTest", result[0][0].FullyQualifiedName);
            Assert.AreEqual(2, result[1].Count);
            Assert.AreEqual(1, result[2].Count);
        }

        [TestMethod]
        public void AsymmetricCase_TestsAreDistributedCorrectly()
        {
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            durations.Add(ToTestCase("ShortTest1"), 1);
            durations.Add(ToTestCase("LongTest"), 5);

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(3);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual("LongTest", result[0][0].FullyQualifiedName);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void RandomTestDurations_TestsAreDistributedCorrectly()
        {
            ExecuteRandomDurationsTest(5000, 1000, 8);
            ExecuteRandomDurationsTest(5000, 500, 7);
            ExecuteRandomDurationsTest(50, 100000, 8);
        }

        private void ExecuteRandomDurationsTest(int nrOfTests, int maxRandomDuration, int nrOfThreads)
        {
            IDictionary<TestCase, int> durations = CreateRandomTestResults(nrOfTests, maxRandomDuration);

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(nrOfThreads);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(nrOfThreads, result.Count);
            Assert.AreEqual(nrOfTests, result.Select(l => l.Count).Sum());

            int sumOfAllDurations = durations.Select(kvp => kvp.Value).Sum();
            int maxDuration = durations.Select(kvp => kvp.Value).Max();

            int targetDuration = sumOfAllDurations / nrOfThreads;

            HashSet<TestCase> foundTestcases = new HashSet<TestCase>();
            foreach (List<TestCase> testcases in result)
            {
                int sum = testcases.Select(tc => durations[tc]).Sum();
                Assert.IsTrue(sum < targetDuration + maxDuration / 2);
                Assert.IsTrue(sum > targetDuration - maxDuration / 2);

                foundTestcases.UnionWith(testcases);
            }

            Assert.AreEqual(nrOfTests, foundTestcases.Count);
        }

        private IDictionary<TestCase, int> CreateRandomTestResults(int nr, int maxDuration)
        {
            IDictionary<TestCase, int> durations = new Dictionary<TestCase, int>();
            Random random = new Random();
            for (int i = 0; i < nr; i++)
            {
                durations.Add(ToTestCase("Suite.Test" + i), random.Next(1, maxDuration));
            }
            return durations;
        }

    }

}