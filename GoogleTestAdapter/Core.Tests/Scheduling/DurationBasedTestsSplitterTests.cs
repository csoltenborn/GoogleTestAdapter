using System.Collections.Generic;
using System;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class DurationBasedTestsSplitterTests : TestsBase
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_SimpleCase_TestsAreDistributedCorrectly()
        {
            IDictionary<Model.TestCase, int> durations = new Dictionary<Model.TestCase, int>
            {
                { TestDataCreator.ToTestCase("ShortTest1"), 1 },
                { TestDataCreator.ToTestCase("ShortTest2"), 1 },
                { TestDataCreator.ToTestCase("LongTest"), 3 },
                { TestDataCreator.ToTestCase("ShortTest3"), 1 }
            };

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Should().HaveCount(2);
            result[0].Should().ContainSingle();
            result[0][0].FullyQualifiedName.Should().Be("LongTest");
            result[1].Should().HaveCount(3);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_SimpleCaseWithThreeThreads_TestsAreDistributedCorrectly()
        {
            IDictionary<Model.TestCase, int> durations = new Dictionary<Model.TestCase, int>
            {
                { TestDataCreator.ToTestCase("ShortTest1"), 1 },
                { TestDataCreator.ToTestCase("ShortTest2"), 1 },
                { TestDataCreator.ToTestCase("LongTest"), 3 },
                { TestDataCreator.ToTestCase("ShortTest3"), 1 }
            };

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(3);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Should().HaveCount(3);
            result[0].Should().ContainSingle();
            result[0][0].FullyQualifiedName.Should().Be("LongTest");
            result[1].Should().HaveCount(2);
            result[2].Should().ContainSingle();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_AsymmetricCase_TestsAreDistributedCorrectly()
        {
            IDictionary<Model.TestCase, int> durations = new Dictionary<Model.TestCase, int>
            {
                { TestDataCreator.ToTestCase("ShortTest1"), 1 },
                { TestDataCreator.ToTestCase("LongTest"), 5 }
            };

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(3);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Should().HaveCount(2);
            result[0].Should().ContainSingle();
            result[0][0].FullyQualifiedName.Should().Be("LongTest");
            result[1].Should().ContainSingle();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_RandomTestDurations_TestsAreDistributedCorrectly()
        {
            ExecuteRandomDurationsTest(5000, 1000, 8);
            ExecuteRandomDurationsTest(5000, 500, 7);
            ExecuteRandomDurationsTest(50, 100000, 8);
        }


        private void ExecuteRandomDurationsTest(int nrOfTests, int maxRandomDuration, int nrOfThreads)
        {
            IDictionary<Model.TestCase, int> durations = CreateRandomTestResults(nrOfTests, maxRandomDuration);

            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(nrOfThreads);

            ITestsSplitter splitter = new DurationBasedTestsSplitter(durations, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Should().HaveCount(nrOfThreads);
            result.Select(l => l.Count).Sum().Should().Be(nrOfTests);

            int sumOfAllDurations = durations.Select(kvp => kvp.Value).Sum();
            int maxDuration = durations.Select(kvp => kvp.Value).Max();

            int targetDuration = sumOfAllDurations / nrOfThreads;

            HashSet<Model.TestCase> foundTestcases = new HashSet<Model.TestCase>();
            foreach (List<Model.TestCase> testcases in result)
            {
                int sum = testcases.Select(tc => durations[tc]).Sum();
                sum.Should().BeLessThan(targetDuration + maxDuration / 2);
                sum.Should().BeGreaterThan(targetDuration - maxDuration / 2);

                foundTestcases.UnionWith(testcases);
            }

            foundTestcases.Should().HaveCount(nrOfTests);
        }

        private IDictionary<Model.TestCase, int> CreateRandomTestResults(int nr, int maxDuration)
        {
            IDictionary<Model.TestCase, int> durations = new Dictionary<Model.TestCase, int>();
            Random random = new Random();
            for (int i = 0; i < nr; i++)
            {
                durations.Add(TestDataCreator.ToTestCase("Suite.Test" + i), random.Next(1, maxDuration));
            }
            return durations;
        }

    }

}