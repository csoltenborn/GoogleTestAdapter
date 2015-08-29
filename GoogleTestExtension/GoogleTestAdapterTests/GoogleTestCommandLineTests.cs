using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestCommandLineTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void TestArgumentsWhenRunningAllTests()
        {
            string CommandLine = new GoogleTestCommandLine(true, new List<TestCase>(), new List<TestCase>(), "", MockLogger.Object, MockOptions.Object).GetCommandLine();

            Assert.AreEqual("--gtest_output=\"xml:\" ", CommandLine);
        }

        [TestMethod]
        public void TestCombinesCommonTestsInSuite()
        {
            string[] TestsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest" };
            IEnumerable<TestCase> TestCases = TestsWithCommonSuite.Select(ToTestCase);

            string CommandLine = new GoogleTestCommandLine(false, TestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLine();

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:", CommandLine);
        }

        [TestMethod]
        public void CombinesCommonTestsInSuiteInDifferentOrder()
        {
            string[] TestsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.gsdfgdfgsdfg", "FooSuite.23453452345", "FooSuite.bxcvbxcvbxcvb" };
            IEnumerable<TestCase> TestCases = TestsWithCommonSuite.Select(ToTestCase);
            IEnumerable<TestCase> TestCasesBackwards = TestCases.Reverse();

            string CommandLine = new GoogleTestCommandLine(false, TestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLine();
            string CommandLineFromBackwards = new GoogleTestCommandLine(false, TestCasesBackwards, TestCasesBackwards, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLine();

            string ExpectedCommandLine = "--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:";
            Assert.AreEqual(ExpectedCommandLine, CommandLine);
            Assert.AreEqual(ExpectedCommandLine, CommandLineFromBackwards);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite()
        {
            string[] TestsWithDifferentSuite = new string[] { "FooSuite.BarTest", "BarSuite.BazTest1" };
            string[] AllTests = new string[] { "FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest1", "BarSuite.BazTest2" };
            IEnumerable<TestCase> TestCases = TestsWithDifferentSuite.Select(ToTestCase);
            IEnumerable<TestCase> AllTestCases = AllTests.Select(ToTestCase);

            string CommandLine = new GoogleTestCommandLine(false, AllTestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLine();

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1", CommandLine);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite_InDifferentOrder()
        {
            string[] TestsWithDifferentSuite = new string[] { "BarSuite.BazTest1", "FooSuite.BarTest" };
            string[] AllTests = new string[] { "BarSuite.BazTest1", "FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest2" };
            IEnumerable<TestCase> TestCases = TestsWithDifferentSuite.Select(ToTestCase);
            IEnumerable<TestCase> AllTestCases = AllTests.Select(ToTestCase);

            string CommandLine = new GoogleTestCommandLine(false, AllTestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLine();

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=BarSuite.BazTest1:FooSuite.BarTest", CommandLine);
        }

    }
}