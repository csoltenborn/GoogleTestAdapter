using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class CommandLineGeneratorTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        public void AppendsAdditionalArgumentsCorrectly()
        {
            string userParameters = "-testdirectory=\"MyTestDirectory\"";

            string commandLine = new CommandLineGenerator(true, new List<TestCase>(), new List<TestCase>(), DummyExecutable.Length, userParameters, "", MockLogger.Object, MockOptions.Object).GetCommandLines().First().CommandLine;

            Assert.IsTrue(commandLine.EndsWith(" -testdirectory=\"MyTestDirectory\""));
        }

        [TestMethod]
        public void TestArgumentsWhenRunningAllTests()
        {
            string commandLine = new CommandLineGenerator(true, new List<TestCase>(), new List<TestCase>(), DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object).GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\"", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void TestCombinesCommonTestsInSuite()
        {
            IEnumerable<TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");

            string commandLine = new CommandLineGenerator(false, testCasesWithCommonSuite, testCasesWithCommonSuite, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void CombinesCommonTestsInSuiteInDifferentOrder()
        {
            IEnumerable<TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest",
                "FooSuite.gsdfgdfgsdfg", "FooSuite.23453452345", "FooSuite.bxcvbxcvbxcvb");
            IEnumerable<TestCase> testCasesReversed = testCasesWithCommonSuite.Reverse();

            string commandLine = new CommandLineGenerator(false, testCasesWithCommonSuite, testCasesWithCommonSuite, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First().CommandLine;
            string commandLineFromBackwards = new CommandLineGenerator(false, testCasesReversed, testCasesReversed, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First().CommandLine;

            string ExpectedCommandLine = "--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:";
            Assert.AreEqual(ExpectedCommandLine, commandLine);
            Assert.AreEqual(ExpectedCommandLine, commandLineFromBackwards);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite()
        {
            IEnumerable<TestCase> testCasesWithDifferentSuite = CreateDummyTestCases("FooSuite.BarTest", "BarSuite.BazTest1");
            IEnumerable<TestCase> allTestCases = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest1", "BarSuite.BazTest2");

            string commandLine = new CommandLineGenerator(false, allTestCases, testCasesWithDifferentSuite, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1", commandLine);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite_InDifferentOrder()
        {
            IEnumerable<TestCase> testCasesWithDifferentSuite = CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest");
            IEnumerable<TestCase> allTestCases = CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest", "FooSuite.BazTest", "BarSuite.BazTest2");

            string commandLine = new CommandLineGenerator(false, allTestCases, testCasesWithDifferentSuite, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=BarSuite.BazTest1:FooSuite.BarTest", commandLine);
        }

        [TestMethod]
        public void BreaksUpLongCommandLinesCorrectly()
        {
            List<string> allTests = new List<string>();
            List<string> testsToExecute = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                allTests.Add("MyTestSuite" + i + ".MyTest");
                testsToExecute.Add("MyTestSuite" + i + ".MyTest");
                allTests.Add("MyTestSuite" + i + ".MyTest2");
            }
            IEnumerable<TestCase> allTestCases = allTests.Select(ToTestCase).ToList();
            IEnumerable<TestCase> testCases = testsToExecute.Select(ToTestCase).ToList();

            List<CommandLineGenerator.Args> commands = new CommandLineGenerator(false, allTestCases, testCases, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().ToList();

            Assert.AreEqual(3, commands.Count);

            int lengthOfLongestTestname = allTests.Max(s => s.Length);

            string commandLine = commands[0].CommandLine;
            Assert.IsTrue(commandLine.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(commandLine.Length >= CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - DummyExecutable.Length - 1);
            Assert.IsTrue(commandLine.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite0.MyTest:"));

            commandLine = commands[1].CommandLine;
            Assert.IsTrue(commandLine.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(commandLine.Length >= CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - DummyExecutable.Length - 1);
            Assert.IsTrue(commandLine.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));

            commandLine = commands[2].CommandLine;
            Assert.IsTrue(commandLine.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(commandLine.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));

            HashSet<TestCase> testsAsSet = new HashSet<TestCase>(testCases);
            HashSet<TestCase> splittedTestsAsSet = new HashSet<TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            Assert.AreEqual(testsAsSet.Count, splittedTestsAsSet.Count);
            foreach (TestCase testCase in testsAsSet)
            {
                Assert.IsTrue(splittedTestsAsSet.Contains(testCase));
            }
        }

        [TestMethod]
        public void BreaksUpLongCommandLinesWithSuitesCorrectly()
        {
            List<string> allTests = new List<string>();
            List<string> testsToExecute = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                allTests.Add("MyTestSuite" + i + ".MyTest");
                testsToExecute.Add("MyTestSuite" + i + ".MyTest");
                allTests.Add("MyTestSuite" + i + ".MyTest2");
            }
            testsToExecute.Add("MyTestSuite1.MyTest2");
            testsToExecute.Add("MyTestSuite5.MyTest2");

            IEnumerable<TestCase> allTestCases = allTests.Select(ToTestCase).ToList();
            IEnumerable<TestCase> testCases = testsToExecute.Select(ToTestCase).ToList();

            List<CommandLineGenerator.Args> commands = new CommandLineGenerator(false, allTestCases, testCases, DummyExecutable.Length, "", "", MockLogger.Object, MockOptions.Object)
                .GetCommandLines().ToList();

            Assert.AreEqual(3, commands.Count);

            int lengthOfLongestTestname = allTests.Max(s => s.Length);

            string command = commands[0].CommandLine;
            Assert.IsTrue(command.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(command.Length >= CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - DummyExecutable.Length - 1);
            Assert.IsTrue(command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:MyTestSuite0.MyTest:"));

            command = commands[1].CommandLine;
            Assert.IsTrue(command.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(command.Length >= CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - DummyExecutable.Length - 1);
            Assert.IsFalse(command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:"));
            Assert.IsTrue(command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));

            command = commands[2].CommandLine;
            Assert.IsTrue(command.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsFalse(command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:"));
            Assert.IsTrue(command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter="));

            HashSet<TestCase> testsAsSet = new HashSet<TestCase>(testCases);
            HashSet<TestCase> splittedTestsAsSet = new HashSet<TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            Assert.AreEqual(testsAsSet.Count, splittedTestsAsSet.Count);
            foreach (TestCase testCase in testsAsSet)
            {
                Assert.IsTrue(splittedTestsAsSet.Contains(testCase));
            }
        }

    }

}