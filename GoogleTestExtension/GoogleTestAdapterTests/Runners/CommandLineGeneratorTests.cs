using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// ReSharper disable PossibleMultipleEnumeration

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class CommandLineGeneratorTests : AbstractGoogleTestExtensionTests
    {

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowsIfUserParametersIsNull()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new CommandLineGenerator(new List<Model.TestCase>(), new List<Model.TestCase>(), 0, null, "", TestEnvironment);
        }

        [TestMethod]
        public void AppendsAdditionalArgumentsCorrectly()
        {
            string userParameters = "-testdirectory=\"MyTestDirectory\"";

            string commandLine = new CommandLineGenerator(new List<Model.TestCase>(), new List<Model.TestCase>(), DummyExecutable.Length, userParameters, "", TestEnvironment).GetCommandLines().First().CommandLine;

            Assert.IsTrue(commandLine.EndsWith(" -testdirectory=\"MyTestDirectory\""));
        }

        [TestMethod]
        public void CorrectArgumentsWhenRunningAllTests()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1 param", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\"", commandLine);
        }

        [TestMethod]
        public void AppendsRepetitionsOption()
        {
            MockOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);

            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            string repetitionsOption = GoogleTestConstants.NrOfRepetitionsOption + "=4711";
            Assert.AreEqual("--gtest_output=\"xml:\"" + repetitionsOption, commandLine);
        }

        [TestMethod]
        public void AppendsShuffleTestOptionWithDefaultSeed()
        {
            MockOptions.Setup(o => o.ShuffleTests).Returns(true);

            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\"" + GoogleTestConstants.ShuffleTestsOption, commandLine);
        }

        [TestMethod]
        public void AppendsShuffleTestOptionWithFixedSeed()
        {
            MockOptions.Setup(o => o.ShuffleTests).Returns(true);
            MockOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);

            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            string shuffleTestsOption = GoogleTestConstants.ShuffleTestsOption
                + GoogleTestConstants.ShuffleTestsSeedOption + "=4711";
            Assert.AreEqual("--gtest_output=\"xml:\"" + shuffleTestsOption, commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void CombinesCommonTestsInSuite()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithCommonSuite.Union(CreateDummyTestCases("BarSuite.FooTest"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithCommonSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void CombinesCommonParameterizedTestsInSuite()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases(
                "InstantiationName2/ParameterizedTests.SimpleTraits/0  # GetParam() = (1,)",
                "InstantiationName/ParameterizedTests.SimpleTraits/0  # GetParam() = (1,)",
                "InstantiationName/ParameterizedTests.SimpleTraits/1  # GetParam() = (,2)");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithCommonSuite.Union(CreateDummyTestCases("InstantiationName2/ParameterizedTests.SimpleTraits/1  # GetParam() = (,2)"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithCommonSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=InstantiationName/ParameterizedTests.*:InstantiationName2/ParameterizedTests.SimpleTraits/0", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void CombinesCommonTestsInSuiteInDifferentOrder()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest",
                "FooSuite.gsdfgdfgsdfg", "FooSuite.23453452345", "FooSuite.bxcvbxcvbxcvb");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithCommonSuite.Union(CreateDummyTestCases("BarSuite.BarTest"));
            IEnumerable<Model.TestCase> testCasesReversed = testCasesWithCommonSuite.Reverse();

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithCommonSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;
            string commandLineFromBackwards = new CommandLineGenerator(allTestCases, testCasesReversed, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            string ExpectedCommandLine = "--gtest_output=\"xml:\" --gtest_filter=FooSuite.*:";
            Assert.AreEqual(ExpectedCommandLine, commandLine);
            Assert.AreEqual(ExpectedCommandLine, commandLineFromBackwards);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite()
        {
            IEnumerable<Model.TestCase> testCasesWithDifferentSuite = CreateDummyTestCases("FooSuite.BarTest", "BarSuite.BazTest1");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithDifferentSuite.Union(CreateDummyTestCases("FooSuite.BazTest", "BarSuite.BazTest2"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithDifferentSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\" --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1", commandLine);
        }

        [TestMethod]
        public void DoesNotCombineTestsNotHavingCommonSuite_InDifferentOrder()
        {
            IEnumerable<Model.TestCase> testCasesWithDifferentSuite = CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithDifferentSuite.Union(CreateDummyTestCases("FooSuite.BazTest", "BarSuite.BazTest2"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithDifferentSuite, DummyExecutable.Length, "", "", TestEnvironment)
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
            IEnumerable<Model.TestCase> allTestCases = allTests.Select(ToTestCase).ToList();
            IEnumerable<Model.TestCase> testCases = testsToExecute.Select(ToTestCase).ToList();

            List<CommandLineGenerator.Args> commands = new CommandLineGenerator(allTestCases, testCases, DummyExecutable.Length, "", "", TestEnvironment)
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

            HashSet<Model.TestCase> testsAsSet = new HashSet<Model.TestCase>(testCases);
            HashSet<Model.TestCase> splittedTestsAsSet = new HashSet<Model.TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            Assert.AreEqual(testsAsSet.Count, splittedTestsAsSet.Count);
            foreach (Model.TestCase testCase in testsAsSet)
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

            IEnumerable<Model.TestCase> allTestCases = allTests.Select(ToTestCase).ToList();
            IEnumerable<Model.TestCase> testCases = testsToExecute.Select(ToTestCase).ToList();

            List<CommandLineGenerator.Args> commands = new CommandLineGenerator(allTestCases, testCases, DummyExecutable.Length, "", "", TestEnvironment)
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

            HashSet<Model.TestCase> testsAsSet = new HashSet<Model.TestCase>(testCases);
            HashSet<Model.TestCase> splittedTestsAsSet = new HashSet<Model.TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            Assert.AreEqual(testsAsSet.Count, splittedTestsAsSet.Count);
            foreach (Model.TestCase testCase in testsAsSet)
            {
                Assert.IsTrue(splittedTestsAsSet.Contains(testCase));
            }
        }

    }

}