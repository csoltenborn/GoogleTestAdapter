using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using GoogleTestAdapterUiTests.Helpers;
using GoogleTestAdapter.VsPackage;
using static GoogleTestAdapter.TestMetadata.TestCategories;

namespace GoogleTestAdapterUiTests
{
    [TestClass]
    public class UiTests
    {
        private const string BatchTeardownWarning = "Warning: Test teardown batch returned exit code 1";


        [ClassInitialize]
        public static void SetupVanillaVsExperimentalInstance(TestContext testContext)
        {
            VS.SetupVanillaVsExperimentalInstance("GoogleTestAdapterUiTests");
            VS.LaunchVsExperimentalInstance();
        }

        [TestInitialize]
        public void OpenSolutionAndTestExplorer()
        {
            VS.OpenSolution();
            VS.TestExplorer.OpenTestExplorer();
        }

        [TestCleanup]
        public void CloseSolution()
        {
            VS.CloseSolution();
        }

        [ClassCleanup]
        public static void CleanVsExperimentalInstance()
        {
            VS.CleanVsExperimentalInstance();
        }


        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunAllTests_GlobalAndSolutionSettings_BatchTeardownWarning()
        {
            try
            {
                VS.TestExplorer.RunAllTests();

                VS.GetOutput().Should().Contain(BatchTeardownWarning);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
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
                    VS.TestExplorer.SelectTestSettingsFile(VS.UserSettingsFile);

                    VS.TestExplorer.RunAllTests();

                    string output = VS.GetOutput();
                    output.Should().Contain("--gtest_shuffle");
                    output.Should().Contain("--gtest_repeat=3");
                    output.Should().NotContain(BatchTeardownWarning);
                }
                finally
                {
                    VS.TestExplorer.UnselectTestSettingsFile();
                }
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
            }
        }

        [TestMethod, Ignore]
        [TestCategory(Ui)]
        public void RunAllTests__AllTestsAreRun()
        {
            try
            {
                VS.TestExplorer.RunAllTests();
                new ResultChecker(Path.Combine(VS.UiTestsDirectory, "UITestResults"), Path.Combine(VS.UiTestsDirectory, "TestErrors"), ".xml")
                    .CheckResults(VS.TestExplorer.Parser.ParseTestResults().ToXML(), GetType().Name);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
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
                VS.TestExplorer.RunSelectedTests(displayNames);
                string result = VS.TestExplorer.Parser.ParseTestResults().ToXML();
                new ResultChecker(Path.Combine(VS.UiTestsDirectory, "UITestResults"), Path.Combine(VS.UiTestsDirectory, "TestErrors"), ".xml")
                    // ReSharper disable once ExplicitCallerInfoArgument
                    .CheckResults(result, GetType().Name, testCaseName);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
            }
        }

    }

}