using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class NumberBasedTestsSplitterTests : AbstractGoogleTestExtensionTests
    {
        [TestMethod]
        public void SplitTestcases_SameNumberOfTestsAsThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void SplitTestcases_MoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.FooTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(2, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void SplitTestcases_ALotMoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            List<Model.TestCase> testcases = new List<Model.TestCase>();
            for (int i = 0; i < 5002; i++)
            {
                testcases.Add(ToTestCase("TestSuite.Test" + i));
            }
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testcases, TestEnvironment);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(8, result.Count);
            Assert.AreEqual(626, result[0].Count);
            Assert.AreEqual(626, result[1].Count);
            Assert.AreEqual(625, result[2].Count);
            Assert.AreEqual(625, result[3].Count);
            Assert.AreEqual(625, result[4].Count);
            Assert.AreEqual(625, result[5].Count);
            Assert.AreEqual(625, result[6].Count);
            Assert.AreEqual(625, result[7].Count);
        }

        [TestMethod]
        public void SplitTestcases_MoreThreadsThanTests_TestsAreDistributedEvenly()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment);
            List<List<Model.TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

    }

}