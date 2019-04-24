using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestResults;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;
// ReSharper disable PossibleMultipleEnumeration

namespace GoogleTestAdapter.Runners
{
    [TestClass]
    public class CommandLineGeneratorTests : TestsBase
    {

        private static readonly string DefaultArgs =
            GoogleTestConstants.GetCatchExceptionsOption(SettingsWrapper.OptionCatchExceptionsDefaultValue) +
            GoogleTestConstants.GetBreakOnFailureOption(SettingsWrapper.OptionBreakOnFailureDefaultValue);

        [TestMethod]
        [TestCategory(Unit)]
        public void Constructor_UserParametersNull_Throws()
        {
            Action a =
                () =>
                    // ReSharper disable once ObjectCreationAsStatement
                    new CommandLineGenerator(new List<Model.TestCase>(), 0, null, "",
                        TestEnvironment.Options);
            a.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_AdditionalArguments_AreAppendedCorrectly()
        {
            string userParameters = "-testdirectory=\"MyTestDirectory\"";

            string commandLine = new CommandLineGenerator(new List<Model.TestCase>(), TestDataCreator.DummyExecutable.Length, userParameters, "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            commandLine.Should().EndWith(" -testdirectory=\"MyTestDirectory\"");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_NoTestCases_NoFilter()
        {
            string commandLine = new CommandLineGenerator(new List<Model.TestCase>(), TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            commandLine.Should().NotContain(GoogleTestConstants.FilterOption);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_OnlyExitCodeTestCase_DummyFilter()
        {
            var exitCodeTestName = "ExitCodeTest";
            MockOptions.Setup(o => o.ExitCodeTestCase).Returns(exitCodeTestName);

            var exitCodeTestCase =
                ExitCodeTestsReporter.CreateExitCodeTestCase(MockOptions.Object, TestDataCreator.DummyExecutable);
            string commandLine = new CommandLineGenerator(new List<Model.TestCase> {exitCodeTestCase}, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            commandLine.Should().Contain($"{GoogleTestConstants.FilterOption}GTA_NOT_EXISTING_DUMMY_TEST_CASE");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_AllTests_ProducesCorrectArguments()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Suite1.Test1 param", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            commandLine.Should().Be($"--gtest_output=\"xml:\"{DefaultArgs}");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_CatchExceptionsOption_IsAppendedCorrectly()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;
            string catchExceptionsOption = GoogleTestConstants.GetCatchExceptionsOption(true);
            commandLine.Should().Contain(catchExceptionsOption);

            MockOptions.Setup(o => o.CatchExceptions).Returns(false);

            commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;
            catchExceptionsOption = GoogleTestConstants.GetCatchExceptionsOption(false);

            commandLine.Should().Contain(catchExceptionsOption);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_BreakOnFailureOption_IsAppendedCorrectly()
        {
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;
            string breakOnFailureOption = GoogleTestConstants.GetBreakOnFailureOption(false);
            commandLine.Should().Contain(breakOnFailureOption);

            MockOptions.Setup(o => o.BreakOnFailure).Returns(true);

            commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;
            breakOnFailureOption = GoogleTestConstants.GetBreakOnFailureOption(true);
            commandLine.Should().Contain(breakOnFailureOption);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_RepetitionsOption_IsAppendedCorrectly()
        {
            MockOptions.Setup(o => o.NrOfTestRepetitions).Returns(4711);

            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            string repetitionsOption = GoogleTestConstants.NrOfRepetitionsOption + "=4711";
            commandLine.Should().Be($"--gtest_output=\"xml:\"{DefaultArgs}{repetitionsOption}");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_ShuffleTestsWithDefaultSeed_IsAppendedCorrectly()
        {
            MockOptions.Setup(o => o.ShuffleTests).Returns(true);

            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            commandLine.Should().Be($"--gtest_output=\"xml:\"{DefaultArgs}{GoogleTestConstants.ShuffleTestsOption}");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_ShuffleTestsWithCustomSeed_IsAppendedCorrectly()
        {
            MockOptions.Setup(o => o.ShuffleTests).Returns(true);
            MockOptions.Setup(o => o.ShuffleTestsSeed).Returns(4711);

            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCases("Suite1.Test1", "Suite2.Test2");
            string commandLine = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().First().CommandLine;

            string shuffleTestsOption = GoogleTestConstants.ShuffleTestsOption
                + GoogleTestConstants.ShuffleTestsSeedOption + "=4711";
            commandLine.Should().Be($"--gtest_output=\"xml:\"{DefaultArgs}{shuffleTestsOption}");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_TestsWithCommonSuite_AreCombinedViaSuite()
        {
            string[] testCaseNamesWithCommonSuite = { "FooSuite.BarTest", "FooSuite.BazTest" };
            string[] allTestCaseNames = testCaseNamesWithCommonSuite.Union("BarSuite.FooTest".Yield()).ToArray();
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCasesFull(testCaseNamesWithCommonSuite, allTestCaseNames);

            string commandLine = new CommandLineGenerator(testCasesWithCommonSuite, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().First().CommandLine;

            commandLine.Should().Be($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=FooSuite.*:");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_ParameterizedTestsWithCommonSuite_AreCombinedViaSuite()
        {
            string[] testCaseNamesWithCommonSuite =
            {
                "InstantiationName2/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/1"
            };
            string[] allTestCaseNames = testCaseNamesWithCommonSuite
                .Union("InstantiationName2/ParameterizedTests.SimpleTraits/1  # GetParam() = (,2)".Yield())
                .ToArray();
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCasesFull(testCaseNamesWithCommonSuite, allTestCaseNames);

            string commandLine = new CommandLineGenerator(testCasesWithCommonSuite, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().First().CommandLine;

            commandLine.Should()
                .Be(
                    $"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=InstantiationName/ParameterizedTests.*:InstantiationName2/ParameterizedTests.SimpleTraits/0");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_TestsWithCommonSuiteInReverseOrder_AreCombinedViaSuite()
        {
            string[] testCaseNamesWithCommonSuite = { "FooSuite.BarTest", "FooSuite.BazTest",
                "FooSuite.gsdfgdfgsdfg", "FooSuite.23453452345", "FooSuite.bxcvbxcvbxcvb" };
            string[] allTestCaseNames = testCaseNamesWithCommonSuite.Union("BarSuite.BarTest".Yield()).ToArray();
            IEnumerable<Model.TestCase> testCasesWithCommonSuite = TestDataCreator.CreateDummyTestCasesFull(testCaseNamesWithCommonSuite, allTestCaseNames);

            IEnumerable<Model.TestCase> testCasesReversed = testCasesWithCommonSuite.Reverse();

            string commandLine = new CommandLineGenerator(testCasesWithCommonSuite, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().First().CommandLine;
            string commandLineFromBackwards = new CommandLineGenerator(testCasesReversed, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().First().CommandLine;

            string expectedCommandLine = $"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=FooSuite.*:";
            commandLine.Should().Be(expectedCommandLine);
            commandLineFromBackwards.Should().Be(expectedCommandLine);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_TestsWithoutCommonSuite_AreNotCombined()
        {
            string[] testCaseNamesWithDifferentSuite = { "FooSuite.BarTest", "BarSuite.BazTest1" };
            string[] allTestCaseNames = testCaseNamesWithDifferentSuite.Union(new[]{ "FooSuite.BazTest", "BarSuite.BazTest2" }).ToArray();
            IEnumerable<Model.TestCase> testCasesWithDifferentSuite = TestDataCreator.CreateDummyTestCasesFull(testCaseNamesWithDifferentSuite, allTestCaseNames);

            string commandLine = new CommandLineGenerator(testCasesWithDifferentSuite, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().First().CommandLine;

            commandLine.Should()
                .Be($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=FooSuite.BarTest:BarSuite.BazTest1");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_TestsWithoutCommonSuiteInDifferentOrder_AreNotCombined()
        {
            string[] testCaseNamesWithDifferentSuite = { "BarSuite.BazTest1", "FooSuite.BarTest" };
            string[] allTestCaseNames = testCaseNamesWithDifferentSuite.Union(new[] { "FooSuite.BazTest", "BarSuite.BazTest2" }).ToArray();
            IEnumerable<Model.TestCase> testCasesWithDifferentSuite = TestDataCreator.CreateDummyTestCasesFull(testCaseNamesWithDifferentSuite, allTestCaseNames);

            string commandLine = new CommandLineGenerator(testCasesWithDifferentSuite, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().First().CommandLine;

            commandLine.Should()
                .Be($"--gtest_output=\"xml:\"{DefaultArgs} --gtest_filter=BarSuite.BazTest1:FooSuite.BarTest");
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_ManyTests_BreaksUpLongCommandLinesCorrectly()
        {
            List<string> allTests = new List<string>();
            List<string> testsToExecute = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                string test1 = $"MyTestSuite{i}.MyTest";
                string test2 = $"MyTestSuite{i}.MyTest2";

                allTests.Add(test1);
                testsToExecute.Add(test1);
                allTests.Add(test2);
            }
            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCasesFull(testsToExecute.ToArray(), allTests.ToArray());

            List<CommandLineGenerator.Args> commands = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().ToList();

            commands.Should().HaveCount(3);

            int lengthOfLongestTestname = allTests.Max(s => s.Length);
            int maxLength = CommandLineGenerator.MaxCommandLength - TestDataCreator.DummyExecutable.Length;
            int minLength = CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - TestDataCreator.DummyExecutable.Length - 1;
            string commonStart = $@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=MyTestSuite0.MyTest:";

            string commandLine = commands[0].CommandLine;
            commandLine.Length.Should().BeLessThan(maxLength);
            commandLine.Length.Should().BeGreaterOrEqualTo(minLength);
            commandLine.Should().StartWith(commonStart);

            commandLine = commands[1].CommandLine;
            commandLine.Length.Should().BeLessThan(maxLength);
            commandLine.Length.Should().BeGreaterOrEqualTo(minLength);
            commandLine.Should().StartWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=");

            commandLine = commands[2].CommandLine;
            commandLine.Length.Should().BeLessThan(maxLength);
            commandLine.Should().StartWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=");

            HashSet<Model.TestCase> testsAsSet = new HashSet<Model.TestCase>(testCases);
            HashSet<Model.TestCase> splittedTestsAsSet = new HashSet<Model.TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            splittedTestsAsSet.Should().BeEquivalentTo(testsAsSet);
        }

        [TestMethod]
        [TestCategory(Unit)]
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

            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCasesFull(testsToExecute.ToArray(), allTests.ToArray());

            List<CommandLineGenerator.Args> commands = new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options)
                .GetCommandLines().ToList();

            commands.Should().HaveCount(3);

            int lengthOfLongestTestname = allTests.Max(s => s.Length);
            int maxLength = CommandLineGenerator.MaxCommandLength - TestDataCreator.DummyExecutable.Length;
            int minLength = CommandLineGenerator.MaxCommandLength - lengthOfLongestTestname - TestDataCreator.DummyExecutable.Length - 1;

            string commandLine = commands[0].CommandLine;
            commandLine.Length.Should().BeLessThan(maxLength);
            commandLine.Length.Should().BeGreaterOrEqualTo(minLength);
            commandLine.Should().StartWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:MyTestSuite0.MyTest:");

            commandLine = commands[1].CommandLine;
            commandLine.Length.Should().BeLessThan(maxLength);
            commandLine.Length.Should().BeGreaterOrEqualTo(minLength);
            commandLine.Should().NotStartWith(@"--gtest_output=""xml:"" --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:");
            commandLine.Should().StartWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=");

            commandLine = commands[2].CommandLine;
            commandLine.Length.Should().BeLessThan(maxLength);
            commandLine.Should()
                .NotStartWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=MyTestSuite1.*:MyTestSuite5.*:");
            commandLine.Should().StartWith($@"--gtest_output=""xml:""{DefaultArgs} --gtest_filter=");

            HashSet<Model.TestCase> testsAsSet = new HashSet<Model.TestCase>(testCases);
            HashSet<Model.TestCase> splittedTestsAsSet = new HashSet<Model.TestCase>(commands[0].TestCases.Union(commands[1].TestCases).Union(commands[2].TestCases));

            splittedTestsAsSet.Should().BeEquivalentTo(testsAsSet);
        }

        [TestMethod]
        [TestCategory(Unit)]
        public void GetCommandLines_HugeNumberOfSuitesToBeExecutedFully_WontBreakCommandLineCreation()
        {
            // we create 400 testsuites, with 380 of them holding 2 test-cases, 20 of them ("MyTestSuite..."[0-19]) holding three test-cases (named Test,Test2,Test3)
            var range = Enumerable.Range(0, 400);
            const string baseSuiteName = "MyTestSuiteWithSomeWhatLongNameIsStillHandledCorrectly";
            var allTests = range.Select(idx => baseSuiteName + idx + ".Test")
                   .Concat(range.Select(idx => baseSuiteName + idx + ".Test2"))
                   .Concat(range.Take(20).Select(idx => baseSuiteName + idx + ".Test3"))
                   .ToList();

            // 305 of the 380 2-test-suites will be executed fully (i.e. all test-cases are executed), only one 3-test-suite is executed fully (which holds also the only Test3-case to be execute)
            var testsToExecute = allTests.Where((testcase, idx) => idx < 700 || (idx+5) % 20 == 0).ToList();

            IEnumerable<Model.TestCase> testCases = TestDataCreator.CreateDummyTestCasesFull(testsToExecute.ToArray(), allTests.ToArray());

            List<CommandLineGenerator.Args> commands =
                new CommandLineGenerator(testCases, TestDataCreator.DummyExecutable.Length, "", "", TestEnvironment.Options).GetCommandLines().ToList();

            int longestSuiteName = baseSuiteName.Length + 6; // 6 = 3 for suiteIndex + 3 for suite-filter-delimiter
            int minimumLineLength = CommandLineGenerator.MaxCommandLength - longestSuiteName;

            CommandLineGenerator.MaxCommandLength.Should().Be(8191, "if the maximum command line length changes, we may need to adapt the following expectations");
            commands.Should().HaveCount(4, "based on given test-cases and maximum command line length we expect 4 command-lines to be created");
            commands.First().CommandLine.Should().NotMatchRegex(baseSuiteName + @"(\d|1[^5])\.", "none fo the 3-test-suites (0-19) expect suite 15 must be part of a command line");
            commands.First().TestCases.Count(testCase => testCase.DisplayName.EndsWith("Test3")).Should().Be(1,"the single fully executed 3-test-suite has to be part of the first command line"); 
            commands.Skip(1).Select(cmd => cmd.TestCases.Count(testCase => testCase.DisplayName.EndsWith("Test3"))).Sum().Should().Be(0, "only a single Test3 must be executed on the first command line");
            commands.Take(3).Select(cmd => cmd.CommandLine.Length).Should().OnlyContain(length => length >= minimumLineLength, "all command lines expect the last need to have a minimum length, so no superfluous command-lines are executed");
            commands.Select(cmd => cmd.TestCases.Count).Sum().Should().Be(testsToExecute.Count(), "the number of test to be executed must be identical to those on the command lines");
        }
    }
}