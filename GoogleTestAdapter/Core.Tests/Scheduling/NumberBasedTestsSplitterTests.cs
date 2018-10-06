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

            result.Should().HaveCount(2);
            result[0].Should().ContainSingle();
            result[1].Should().ContainSingle();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_MoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.FooTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Should().HaveCount(2);
            result[0].Should().HaveCount(2);
            result[1].Should().ContainSingle();
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

            result.Should().HaveCount(8);
            result[0].Should().HaveCount(626);
            result[1].Should().HaveCount(626);
            result[2].Should().HaveCount(625);
            result[3].Should().HaveCount(625);
            result[4].Should().HaveCount(625);
            result[5].Should().HaveCount(625);
            result[6].Should().HaveCount(625);
            result[7].Should().HaveCount(625);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void SplitTestcases_MoreThreadsThanTests_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment.Options);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            result.Should().HaveCount(2);
            result[0].Should().ContainSingle();
            result[1].Should().ContainSingle();
        }

    }

}