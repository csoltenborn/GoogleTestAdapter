// This file has been modified by Microsoft on 6/2017.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.EndToEnd.VisualStudio;
using GoogleTestAdapter.Tests.Common.ResultChecker;
using GoogleTestAdapter.Tests.Common.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapterUiTests
{
    [TestClass]
    public class UiTests
    {
        private const string BatchTeardownWarning = "Warning: Test teardown batch returned exit code 1";

        private static Vsui Vsui;

        [ClassInitialize]
        public static void SetupVanillaVsExperimentalInstance(TestContext testContext)
        {
            Vsui = new Vsui();
            throw new NotImplementedException("UI tests are not functional in the current version.");
        }

        [TestInitialize]
        public void OpenSolutionAndTestExplorer()
        {
            Vsui.OpenSolution();
            Vsui.TestExplorer.OpenTestExplorer();
        }

        [TestCleanup]
        public void CloseSolution()
        {
            Vsui.CloseSolution();
        }

        [ClassCleanup]
        public static void CleanVsExperimentalInstance()
        {
        }


        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunAllTests_GlobalAndSolutionSettings_BatchTeardownWarning()
        {
            try
            {
                Vsui.TestExplorer.RunAllTests();

                Vsui.GetOutput().Should().Contain(BatchTeardownWarning);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow(typeof(UiTests).Name);
            }
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunAllTests_UserSettings_ShuffledTestExecutionAndNoBatchWarning()
        {
            try
            {
                try
                {
                    Vsui.TestExplorer.SelectTestSettingsFile(Vsui.UserSettingsFile);

                    Vsui.TestExplorer.RunAllTests();

                    string output = Vsui.GetOutput();
                    output.Should().Contain("--gtest_shuffle");
                    output.Should().Contain("--gtest_repeat=3");
                    output.Should().NotContain(BatchTeardownWarning);
                }
                finally
                {
                    Vsui.TestExplorer.UnselectTestSettingsFile();
                }
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow(typeof(UiTests).Name);
            }
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunAllTests__AllTestsAreRun()
        {
            try
            {
                Vsui.TestExplorer.RunAllTests();
                new ResultChecker(Path.Combine(Vsui.UiTestsDirectory, "UITestResults"), Path.Combine(Vsui.UiTestsDirectory, "TestErrors"), ".xml")
                    .CheckResults(Vsui.TestExplorer.Parser.ParseTestResults().ToXML(), GetType().Name);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow(typeof(UiTests).Name);
            }
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_Crashing_AddPasses()
        {
            RunTest("Crashing.AddPassesAfterCrash");
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_ParameterizedTests_Simple_0()
        {
            RunTest("ParameterizedTests.Simple/0");
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_InstantiationName_ParameterizedTests_SimpleTraits_0()
        {
            RunTest("InstantiationName/ParameterizedTests.SimpleTraits/0");
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_PointerParameterizedTests_CheckStringLength_0()
        {
            RunTest("PointerParameterizedTests.CheckStringLength/0");
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_TypedTests_0_CanIterate()
        {
            RunTest("TypedTests/0.CanIterate");
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_Arr_TypeParameterizedTests_1_CanDefeatMath()
        {
            RunTest("Arr/TypeParameterizedTests/1.CanDefeatMath");
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunSelectedTests_MultipleTests()
        {
            RunTests(new[] { "Crashing.AddPassesAfterCrash", "ParameterizedTests.Simple/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0", "PointerParameterizedTests.CheckStringLength/0",
                "TypedTests/0.CanIterate", "Arr/TypeParameterizedTests/1.CanDefeatMath" });
        }


        private void RunTest(string displayName, [CallerMemberName] string testCaseName = null)
        {
            // ReSharper disable once ExplicitCallerInfoArgument
            RunTests(new[] { displayName }, testCaseName);
        }

        private void RunTests(string[] displayNames, [CallerMemberName] string testCaseName = null)
        {
            try
            {
                Vsui.TestExplorer.RunSelectedTests(displayNames);
                string result = Vsui.TestExplorer.Parser.ParseTestResults().ToXML();
                new ResultChecker(Path.Combine(Vsui.UiTestsDirectory, "UITestResults"), Path.Combine(Vsui.UiTestsDirectory, "TestErrors"), ".xml")
                    // ReSharper disable once ExplicitCallerInfoArgument
                    .CheckResults(result, GetType().Name, testCaseName);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow(typeof(UiTests).Name);
            }
        }

    }

}