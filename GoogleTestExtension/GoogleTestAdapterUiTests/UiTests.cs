using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WPFUIItems;
using GoogleTestAdapterUiTests.Helpers;

namespace GoogleTestAdapterUiTests
{
    [TestClass]
    public class UiTests
    {
        private const bool overwriteTestResults = false;


        private const string BatchTeardownWarning = "Warning: Test teardown batch returned exit code 1";

        private static IUIItem testExplorer;

        [ClassInitialize]
        public static void SetupVanillaVsExperimentalInstance(TestContext testContext)
        {
            VS.SetupVanillaVsExperimentalInstance();
        }

        [TestInitialize]
        public void OpenSolutionAndTestExplorer()
        {
            VS.OpenSolution();

            if (testExplorer == null)
            {
                testExplorer = VS.OpenTestExplorer();
                WaitForTestDiscovery();
            }
            else
            {
                testExplorer = VS.OpenTestExplorer();
            }
        }

        [TestCleanup]
        public void CloseSolution()
        {
            VS.CloseSolution();
        }

        [ClassCleanup]
        public static void CleanVsExperimentalInstance()
        {
            testExplorer = null;
            VS.CleanVsExperimentalInstance();
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunAllTests__AllTestsAreRun()
        {
            try
            {
                // Run all tests and wait till finish (max 1 minute)
                VS.MainWindow.VsMenuBarMenuItems("Test", "Run", "All Tests").Click();
                ProgressBar progressIndicator = testExplorer.Get<ProgressBar>("runProgressBar");
                VS.MainWindow.WaitTill(() => progressIndicator.Value == progressIndicator.Maximum, TimeSpan.FromMinutes(1));

                CheckResults();
            }
            catch (AutomationException exception)
            {
                LogExceptionAndThrow(exception);
            }
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_Crashing_AddPasses()
        {
            ExecuteSingleTestCase("Crashing.AddPasses");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_ParameterizedTests_Simple_0()
        {
            ExecuteSingleTestCase("ParameterizedTests.Simple/0");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_InstantiationName_ParameterizedTests_SimpleTraits_0()
        {
            ExecuteSingleTestCase("InstantiationName/ParameterizedTests.SimpleTraits/0");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_PointerParameterizedTests_CheckStringLength_0()
        {
            ExecuteSingleTestCase("PointerParameterizedTests.CheckStringLength/0");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_TypedTests_0_CanIterate()
        {
            ExecuteSingleTestCase("TypedTests/0.CanIterate");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_Arr_TypeParameterizedTests_1_CanDefeatMath()
        {
            ExecuteSingleTestCase("Arr/TypeParameterizedTests/1.CanDefeatMath");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_MultipleTests()
        {
            ExecuteMultipleTestCases(new[] { "Crashing.AddPasses", "ParameterizedTests.Simple/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0", "PointerParameterizedTests.CheckStringLength/0",
                "TypedTests/0.CanIterate", "Arr/TypeParameterizedTests/1.CanDefeatMath" });
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunAllTests_GlobalAndSolutionSettings_BatchTeardownWarning()
        {
            try
            {
                VS.MainWindow.VsMenuBarMenuItems("Test", "Run", "All Tests").Click();
                ProgressBar progressIndicator = testExplorer.Get<ProgressBar>("runProgressBar");
                VS.MainWindow.WaitTill(() => progressIndicator.Value == progressIndicator.Maximum, TimeSpan.FromMinutes(1));

                IUIItem outputWindow = VS.MainWindow.Get(SearchCriteria.ByText("Output").AndByClassName("GenericPane"), TimeSpan.FromSeconds(10));
                string output = outputWindow.Get<TextBox>("WpfTextView").Text;
                Assert.IsTrue(output.Contains(BatchTeardownWarning));
            }
            catch (AutomationException exception)
            {
                LogExceptionAndThrow(exception);
            }
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunAllTests_UserSettings_ShuffledTestExecutionAndNoBatchWarning()
        {
            try
            {
                try
                {
                    VS.SelectTestSettingsFile(VS.UserSettingsFile);

                    VS.MainWindow.VsMenuBarMenuItems("Test", "Run", "All Tests").Click();
                    ProgressBar progressIndicator = testExplorer.Get<ProgressBar>("runProgressBar");
                    VS.MainWindow.WaitTill(() => progressIndicator.Value == progressIndicator.Maximum, TimeSpan.FromMinutes(1));

                    IUIItem outputWindow = VS.MainWindow.Get(SearchCriteria.ByText("Output").AndByClassName("GenericPane"), TimeSpan.FromSeconds(10));
                    string output = outputWindow.Get<TextBox>("WpfTextView").Text;
                    Assert.IsTrue(output.Contains("--gtest_shuffle"));
                    Assert.IsTrue(output.Contains("--gtest_repeat=5"));
                    Assert.IsFalse(output.Contains(BatchTeardownWarning));
                }
                finally
                {
                    VS.UnselectTestSettingsFile();
                }
            }
            catch (AutomationException exception)
            {
                LogExceptionAndThrow(exception);
            }
        }


        private void ExecuteSingleTestCase(string displayName, [CallerMemberName] string testCaseName = null)
        {
            ExecuteMultipleTestCases(new[] { displayName }, testCaseName);
        }

        private void ExecuteMultipleTestCases(string[] displayNames, [CallerMemberName] string testCaseName = null)
        {
            try
            {
                // Run a selected test and wait till finish (max 1 minute)
                TestExplorerUtil util = new TestExplorerUtil(testExplorer);
                util.SelectTestCases(displayNames);
                VS.MainWindow.VsMenuBarMenuItems("Test", "Run", "Selected Tests").Click();
                ProgressBar progressIndicator = testExplorer.Get<ProgressBar>("runProgressBar");
                VS.MainWindow.WaitTill(
                    () => progressIndicator.Value == progressIndicator.Maximum, TimeSpan.FromSeconds(10));

                CheckResults(testCaseName);
            }
            catch (AutomationException exception)
            {
                LogExceptionAndThrow(exception);
            }
        }

        private void WaitForTestDiscovery()
        {
            ProgressBar delayIndicator = testExplorer.Get<ProgressBar>("delayIndicatorProgressBar");
            VS.MainWindow.WaitTill(() => delayIndicator.IsOffScreen);
        }

        private void CheckResults([CallerMemberName] string testCaseName = null)
        {
            string solutionDir = Path.GetDirectoryName(VS.SolutionDirectory);
            IUIItem outputWindow = VS.MainWindow.Get(SearchCriteria.ByText("Output").AndByClassName("GenericPane"), TimeSpan.FromSeconds(10));
            string testResults = new TestRunSerializer().ParseTestResults(solutionDir, testExplorer, outputWindow).ToXML();
            CheckResults(testResults, testCaseName);
        }

        private void CheckResults(string result, string testCaseName)
        {
            string expectationFile = Path.Combine(VS.UiTestsDirectory, "UITestResults", this.GetType().Name + "__" + testCaseName + ".xml");
            string resultFile = Path.Combine(VS.UiTestsDirectory, "TestErrors", this.GetType().Name + "__" + testCaseName + ".xml");

            if (!File.Exists(expectationFile))
            {
                File.WriteAllText(expectationFile, result);
                Assert.Inconclusive("This is the first time this test runs.");
            }

            string expectedResult = File.ReadAllText(expectationFile);
            string msg;
            bool stringsAreEqual = AreEqual(expectedResult, result, out msg);
            if (!stringsAreEqual)
            {
#pragma warning disable CS0162 // Unreachable code (because overwriteTestResults is compile time constant)
                if (overwriteTestResults)
                {
                    File.WriteAllText(expectationFile, result);
                    Assert.Inconclusive("Test results changed and have been overwritten. Differences: " + msg);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(resultFile));
                    File.WriteAllText(resultFile, result);
                    Assert.Fail("Test result doesn't match expectation. Result written to: " + resultFile + ". Differences: " + msg);
                }
#pragma warning restore CS0162
            }
            else if (stringsAreEqual && File.Exists(resultFile))
            {
                File.Delete(resultFile);
            }
        }

        private bool AreEqual(string expectedResult, string result, out string msg)
        {
            // normalize file endings
            expectedResult = Regex.Replace(expectedResult, @"\r\n|\n\r|\n|\r", "\r\n");
            result = Regex.Replace(result, @"\r\n|\n\r|\n|\r", "\r\n");

            bool areEqual = true;
            List<string> messages = new List<string>();
            if (expectedResult.Length != result.Length)
            {
                areEqual = false;
                messages.Add($"Length differs, expected: {expectedResult.Length}, actual: {result.Length}");
            }

            for (int i = 0; i < Math.Min(expectedResult.Length, result.Length); i++)
            {
                if (expectedResult[i] != result[i])
                {
                    areEqual = false;
                    messages.Add($"First difference at position {i}, expected: {expectedResult[i]}, "
                        + "actual: {result[i]}");
                    break;
                }
            }

            msg = string.Join("; ", messages);
            return areEqual;
        }

        private static void LogExceptionAndThrow(AutomationException exception, [CallerMemberName] string testCaseName = null)
        {
            string debugDetailsFile = Path.Combine(VS.UiTestsDirectory, "TestErrors", typeof(UiTests).Name + "__" + testCaseName + "__DebugDetails.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(debugDetailsFile));
            File.WriteAllText(debugDetailsFile, exception.ToString() + "\r\n" + exception.StackTrace + "\r\n" + exception.DebugDetails);
            throw exception;
        }

    }

}