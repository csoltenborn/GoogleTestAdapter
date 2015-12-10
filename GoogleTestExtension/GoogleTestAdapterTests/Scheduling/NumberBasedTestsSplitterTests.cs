using System.Collections.Generic;
using GoogleTestAdapter.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class NumberBasedTestsSplitterTests : AbstractGoogleTestExtensionTests
    {
        [TestMethod]
        public void SameNumberOfTestsAsThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<TestCase2> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment);
            List<List<TestCase2>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void MoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            IEnumerable<TestCase2> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.FooTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(2);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment);
            List<List<TestCase2>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(2, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void ALotMoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            List<TestCase2> testcases = new List<TestCase2>();
            for (int i = 0; i < 5002; i++)
            {
                testcases.Add(ToTestCase("TestSuite.Test" + i));
            }
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testcases, TestEnvironment);
            List<List<TestCase2>> result = splitter.SplitTestcases();

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
        public void MoreThreadsThanTests_TestsAreDistributedEvenly()
        {
            IEnumerable<TestCase2> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            MockOptions.Setup(o => o.MaxNrOfThreads).Returns(8);

            ITestsSplitter splitter = new NumberBasedTestsSplitter(testCasesWithCommonSuite, TestEnvironment);
            List<List<TestCase2>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

    }

}