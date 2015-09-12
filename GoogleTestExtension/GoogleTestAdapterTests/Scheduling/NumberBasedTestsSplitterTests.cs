using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GoogleTestAdapter.Scheduling
{
    [TestClass]
    public class NumberBasedTestsSplitterTests : AbstractGoogleTestExtensionTests
    {
        [TestMethod]
        public void SameNumberOfTestsAsThreads_TestsAreDistributedEvenly()
        {
            string[] testsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest" };
            IEnumerable<TestCase> testcases = testsWithCommonSuite.Select(ToTestCase);

            MockOptions.Setup(O => O.MaxNrOfThreads).Returns(2);

            NumberBasedTestsSplitter splitter = new NumberBasedTestsSplitter(testcases, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void MoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            string[] testsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.FooTest" };
            IEnumerable<TestCase> testcases = testsWithCommonSuite.Select(ToTestCase);

            MockOptions.Setup(O => O.MaxNrOfThreads).Returns(2);

            NumberBasedTestsSplitter splitter = new NumberBasedTestsSplitter(testcases, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(2, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

        [TestMethod]
        public void ALotMoreTestsThanThreads_TestsAreDistributedEvenly()
        {
            List<TestCase> testcases = new List<TestCase>();
            for (int i = 0; i < 5002; i++)
            {
                testcases.Add(ToTestCase("TestSuite.Test" + i));
            }

            MockOptions.Setup(O => O.MaxNrOfThreads).Returns(8);

            NumberBasedTestsSplitter splitter = new NumberBasedTestsSplitter(testcases, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

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
            string[] testsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest" };
            IEnumerable<TestCase> testcases = testsWithCommonSuite.Select(ToTestCase);

            MockOptions.Setup(O => O.MaxNrOfThreads).Returns(8);

            NumberBasedTestsSplitter splitter = new NumberBasedTestsSplitter(testcases, MockOptions.Object);
            List<List<TestCase>> result = splitter.SplitTestcases();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual(1, result[1].Count);
        }

    }

}