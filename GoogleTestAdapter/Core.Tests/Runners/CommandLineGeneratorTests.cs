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

        private static readonly string DefaultArgs =
            GoogleTestConstants.GetCatchExceptionsOption(Options.OptionCatchExceptionsDefaultValue) +
            GoogleTestConstants.GetBreakOnFailureOption(Options.OptionBreakOnFailureDefaultValue);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_UserParametersNull_Throws()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new CommandLineGenerator(new List<Model.TestCase>(), new List<Model.TestCase>(), 0, null, "", TestEnvironment);
        }

        [TestMethod]
        public void GetCommandLines_AdditionalArguments_AreAppendedCorrectly()
        {
            string userParameters = "-testdirectory=\"MyTestDirectory\"";

            string commandLine = new CommandLineGenerator(new List<Model.TestCase>(), new List<Model.TestCase>(), DummyExecutable.Length, userParameters, "", TestEnvironment).GetCommandLines().First().CommandLine;

            Assert.IsTrue(commandLine.EndsWith(" -testdirectory=\"MyTestDirectory\""));
        }

        [TestMethod]
        public void GetCommandLines_AllTests_ProducesCorrectArguments()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1 param", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            Assert.AreEqual("--gtest_output=\"xml:\"" + DefaultArgs, commandLine);
        }

        [TestMethod]
        public void GetCommandLines_CatchExceptionsOption_IsAppendedCorrectly()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;
            string catchExceptionsOption = GoogleTestConstants.GetCatchExceptionsOption(true);
            Assert.IsTrue(commandLine.Contains(catchExceptionsOption), commandLine);

            MockOptions.Setup(o => o.CatchExceptions).Returns(false);

            commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;
            catchExceptionsOption = GoogleTestConstants.GetCatchExceptionsOption(false);
            Assert.IsTrue(commandLine.Contains(catchExceptionsOption), commandLine);
        }

        [TestMethod]
        public void GetCommandLines_BreakOnFailureOption_IsAppendedCorrectly()
        {
            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;
            string breakOnFailureOption = GoogleTestConstants.GetBreakOnFailureOption(false);
            Assert.IsTrue(commandLine.Contains(breakOnFailureOption), commandLine);

            MockOptions.Setup(o => o.BreakOnFailure).Returns(true);

            commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;
            breakOnFailureOption = GoogleTestConstants.GetBreakOnFailureOption(true);
            Assert.IsTrue(commandLine.Contains(breakOnFailureOption), commandLine);
        }

        [TestMethod]
        public void GetCommandLines_RepetitionsOption_IsAppendedCorrectly()
        {
            MockOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);

            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            string repetitionsOption = GoogleTestConstants.NrOfRepetitionsOption + "=4711";
            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs}{repetitionsOption}", commandLine);
        }

        [TestMethod]
        public void GetCommandLines_ShuffleTestsWithDefaultSeed_IsAppendedCorrectly()
        {
            MockOptions.Setup(o => o.ShuffleTests).Returns(true);

            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs}{GoogleTestConstants.ShuffleTestsOption}", commandLine);
        }

        [TestMethod]
        public void GetCommandLines_ShuffleTestsWithCustomSeed_IsAppendedCorrectly()
        {
            MockOptions.Setup(o => o.ShuffleTests).Returns(true);
            MockOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);

            IEnumerable<Model.TestCase> testCases = CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, testCases, DummyExecutable.Length, "", "", TestEnvironment).GetCommandLines().First().CommandLine;

            string shuffleTestsOption = GoogleTestConstants.ShuffleTestsOption
                + GoogleTestConstants.ShuffleTestsSeedOption + "=4711";
            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs}{shuffleTestsOption}", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void GetCommandLines_TestsWithCommonSuite_AreCombinedViaSuite()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithCommonSuite.Union(CreateDummyTestCases("BarSuite.FooTest"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithCommonSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=FooSuite.*:", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void GetCommandLines_ParameterizedTestsWithCommonSuite_AreCombinedViaSuite()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases(
                "InstantiationName2/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/1");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithCommonSuite.Union(CreateDummyTestCases("InstantiationName2/ParameterizedTests.SimpleTraits/1  # GetParam() = (,2)"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithCommonSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=InstantiationName/ParameterizedTests.*:InstantiationName2/ParameterizedTests.SimpleTraits/0", commandLine);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void GetCommandLines_TestsWithCommonSuiteInReverseOrder_AreCombinedViaSuite()
        {
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = CreateDummyTestCases("FooSuite.BarTest", "FooSuite.BazTest",
                "FooSuite.gsdfgdfgsdfg", "FooSuite.23453452345", "FooSuite.bxcvbxcvbxcvb");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithCommonSuite.Union(CreateDummyTestCases("BarSuite.BarTest"));
            IEnumerable<Model.TestCase> testCasesReversed = testCasesWithCommonSuite.Reverse();

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithCommonSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;
            string commandLineFromBackwards = new CommandLineGenerator(allTestCases, testCasesReversed, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            string ExpectedCommandLine = $"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=FooSuite.*:";
            Assert.AreEqual(ExpectedCommandLine, commandLine);
            Assert.AreEqual(ExpectedCommandLine, commandLineFromBackwards);
        }

        [TestMethod]
        public void GetCommandLines_TestsWithoutCommonSuite_AreNotCombined()
        {
            IEnumerable<Model.TestCase> testCasesWithDifferentSuite = CreateDummyTestCases("FooSuite.BarTest", "BarSuite.BazTest1");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithDifferentSuite.Union(CreateDummyTestCases("FooSuite.BazTest", "BarSuite.BazTest2"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithDifferentSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1", commandLine);
        }

        [TestMethod]
        public void GetCommandLines_TestsWithoutCommonSuiteInDifferentOrder_AreNotCombined()
        {
            IEnumerable<Model.TestCase> testCasesWithDifferentSuite = CreateDummyTestCases("BarSuite.BazTest1", "FooSuite.BarTest");
            IEnumerable<Model.TestCase> allTestCases = testCasesWithDifferentSuite.Union(CreateDummyTestCases("FooSuite.BazTest", "BarSuite.BazTest2"));

            string commandLine = new CommandLineGenerator(allTestCases, testCasesWithDifferentSuite, DummyExecutable.Length, "", "", TestEnvironment)
                .GetCommandLines().First().CommandLine;

            Assert.AreEqual($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=BarSuite.BazTest1:FooSuite.BarTest", commandLine);
        }

        [TestMethod]
        public void GetCommandLines_ManyTests_BreaksUpLongCommandLinesCorrectly()
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
            Assert.IsTrue(commandLine.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=MyTestSuite0.MyTest:"));

            commandLine = commands[1].CommandLine;
            Assert.IsTrue(commandLine.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(commandLine.Length >= CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - DummyExecutable.Length - 1);
            Assert.IsTrue(commandLine.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter="));

            commandLine = commands[2].CommandLine;
            Assert.IsTrue(commandLine.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(commandLine.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter="));

            HashSet<Model.TestCase> testsAsSet = new HashSet<Model.TestCase>(testCases);
            HashSet<Model.TestCase> splittedTestsAsSet = new HashSet<Model.TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            Assert.AreEqual(testsAsSet.Count, splittedTestsAsSet.Count);
            foreach (Model.TestCase testCase in testsAsSet)
            {
                Assert.IsTrue(splittedTestsAsSet.Contains(testCase));
            }
        }

        [TestMethod]
        public void GetCommandLines_ManyTestsWithSuites_BreaksUpLongCommandLinesCorrectly()
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
            Assert.IsTrue(command.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:MyTestSuite0.MyTest:"));

            command = commands[1].CommandLine;
            Assert.IsTrue(command.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsTrue(command.Length >= CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - DummyExecutable.Length - 1);
            Assert.IsFalse(command.StartsWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:"));
            Assert.IsTrue(command.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter="));

            command = commands[2].CommandLine;
            Assert.IsTrue(command.Length < CommandLineGenerator.MaxCommandLength - DummyExecutable.Length);
            Assert.IsFalse(command.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:"));
            Assert.IsTrue(command.StartsWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter="));

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