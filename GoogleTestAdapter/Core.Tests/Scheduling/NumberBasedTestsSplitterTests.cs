using System.Collections.Generic;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class NumberBasedTestsSplitterTests : TestsBase
    {
        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_SameNumberOfTestsAsThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Count.Should().Be(2);
            result[0].Count.Should().Be(1);
            result[1].Count.Should().Be(1);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_MoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.FooTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Count.Should().Be(2);
            result[0].Count.Should().Be(2);
            result[1].Count.Should().Be(1);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_ALotMoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            List<Model.TestCase> testcases = new List<Model.TestCase>();
            for (int i = 0; i < 5002; i++)
            {
                testcases.Add(TestDataCreator.ToTestCase("TestSuite.Test" + i));
            }
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testcases, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Count.Should().Be(8);
            result[0].Count.Should().Be(626);
            result[1].Count.Should().Be(626);
            result[2].Count.Should().Be(625);
            result[3].Count.Should().Be(625);
            result[4].Count.Should().Be(625);
            result[5].Count.Should().Be(625);
            result[6].Count.Should().Be(625);
            result[7].Count.Should().Be(625);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_MoreThreadsThanTests_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Count.Should().Be(2);
            result[0].Count.Should().Be(1);
            result[1].Count.Should().Be(1);
        }

    }

}