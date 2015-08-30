using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            string CommandLine = new GoogleTestCommandLine(true, EXECUTABLE.Length, new List<TestCase>(), new List<TestCase>(), "", MockLogger.Object, MockOptions.Object).GetCommandLines().First();

            Assert.AreEqual("--gtest_output=\"xml:\" ", CommandLine);
        }

        [TestMethod]
        public void TestCombinesCommonTestsInSuite()
        {
            string[] TestsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest" };
            IEnumerable<TestCase> TestCases = TestsWithCommonSuite.Select(ToTestCase);

            string CommandLine = new GoogleTestCommandLine(false, EXECUTABLE.Length, TestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First();

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:", CommandLine);
        }

        [TestMethod]
        public void CombinesCommonTestsInSuiteInDifferentOrder()
        {
            string[] TestsWithCommonSuite = new string[] { "FooSuite.BarTest", "FooSuite.BazTest", "FooSuite.gsdfgdfgsdfg", "FooSuite.23453452345", "FooSuite.bxcvbxcvbxcvb" };
            IEnumerable<TestCase> TestCases = TestsWithCommonSuite.Select(ToTestCase);
            IEnumerable<TestCase> TestCasesBackwards = TestCases.Reverse();

            string CommandLine = new GoogleTestCommandLine(false, EXECUTABLE.Length, TestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First();
            string CommandLineFromBackwards = new GoogleTestCommandLine(false, EXECUTABLE.Length, TestCasesBackwards, TestCasesBackwards, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First();

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

            string CommandLine = new GoogleTestCommandLine(false, EXECUTABLE.Length, AllTestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First();

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1", CommandLine);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite_InDifferentOrder()
        {
            string[] TestsWithDifferentSuite = new string[] { "BarSuite.BazTest1", "FooSuite.BarTest" };
            string[] AllTests = new string[] { "BarSuite.BazTest1", "FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest2" };
            IEnumerable<TestCase> TestCases = TestsWithDifferentSuite.Select(ToTestCase);
            IEnumerable<TestCase> AllTestCases = AllTests.Select(ToTestCase);

            string CommandLine = new GoogleTestCommandLine(false, EXECUTABLE.Length, AllTestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First();

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=BarSuite.BazTest1:FooSuite.BarTest", CommandLine);
        }

        [TestMethod]
        public void BreaksUpLongCommandLinesCorrectly()
        {
            List<string> AllTests = new List<string>();
            List<string> TestsToExecute = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                AllTests.Add("MyTestSuite" + i + ".MyTest");
                TestsToExecute.Add("MyTestSuite" + i + ".MyTest");
                AllTests.Add("MyTestSuite" + i + ".MyTest2");
            }
            IEnumerable<TestCase> AllTestCases = AllTests.Select(ToTestCase);
            IEnumerable<TestCase> TestCases = TestsToExecute.Select(ToTestCase);

            List<string> Commands = new GoogleTestCommandLine(false, EXECUTABLE.Length, AllTestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().ToList();

            Assert.AreEqual(3, Commands.Count);

            int LengthOfLongestTestname = AllTests.Max(S => S.Length);

            string CommandLine = Commands[0];
            Assert.IsTrue(CommandLine.Length < GoogleTestCommandLine.MAX_COMMAND_LENGTH - EXECUTABLE.Length);
            Assert.IsTrue(CommandLine.Length >= GoogleTestCommandLine.MAX_COMMAND_LENGTH - LengthOfLongestTestname - EXECUTABLE.Length - 1);
            Assert.IsTrue(CommandLine.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite0.MyTest:"));

            CommandLine = Commands[1];
            Assert.IsTrue(CommandLine.Length < GoogleTestCommandLine.MAX_COMMAND_LENGTH - EXECUTABLE.Length);
            Assert.IsTrue(CommandLine.Length >= GoogleTestCommandLine.MAX_COMMAND_LENGTH - LengthOfLongestTestname - EXECUTABLE.Length - 1);
            Assert.IsTrue(CommandLine.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));

            CommandLine = Commands[2];
            Assert.IsTrue(CommandLine.Length < GoogleTestCommandLine.MAX_COMMAND_LENGTH - EXECUTABLE.Length);
            Assert.IsTrue(CommandLine.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));
        }

        [TestMethod]
        public void BreaksUpLongCommandLinesWithSuitesCorrectly()
        {
            List<string> AllTests = new List<string>();
            List<string> TestsToExecute = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                AllTests.Add("MyTestSuite" + i + ".MyTest");
                TestsToExecute.Add("MyTestSuite" + i + ".MyTest");
                AllTests.Add("MyTestSuite" + i + ".MyTest2");
            }
            TestsToExecute.Add("MyTestSuite1.MyTest2");
            TestsToExecute.Add("MyTestSuite5.MyTest2");

            IEnumerable<TestCase> AllTestCases = AllTests.Select(ToTestCase);
            IEnumerable<TestCase> TestCases = TestsToExecute.Select(ToTestCase);

            List<string> Commands = new GoogleTestCommandLine(false, EXECUTABLE.Length, AllTestCases, TestCases, "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().ToList();

            Assert.AreEqual(3, Commands.Count);

            int LengthOfLongestTestname = AllTests.Max(S => S.Length);

            string Command = Commands[0];
            Assert.IsTrue(Command.Length < GoogleTestCommandLine.MAX_COMMAND_LENGTH - EXECUTABLE.Length);
            Assert.IsTrue(Command.Length >= GoogleTestCommandLine.MAX_COMMAND_LENGTH - LengthOfLongestTestname - EXECUTABLE.Length - 1);
            Assert.IsTrue(Command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:MyTestSuite0.MyTest:"));

            Command = Commands[1];
            Assert.IsTrue(Command.Length < GoogleTestCommandLine.MAX_COMMAND_LENGTH - EXECUTABLE.Length);
            Assert.IsTrue(Command.Length >= GoogleTestCommandLine.MAX_COMMAND_LENGTH - LengthOfLongestTestname - EXECUTABLE.Length - 1);
            Assert.IsFalse(Command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:"));
            Assert.IsTrue(Command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));

            Command = Commands[2];
            Assert.IsTrue(Command.Length < GoogleTestCommandLine.MAX_COMMAND_LENGTH - EXECUTABLE.Length);
            Assert.IsFalse(Command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:"));
            Assert.IsTrue(Command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));
        }

    }
}