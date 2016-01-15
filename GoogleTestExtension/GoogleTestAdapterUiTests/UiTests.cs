using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using GoogleTestAdapterUiTests.Helpers;

namespace GoogleTestAdapterUiTests
{
    [TestClass]
    public class UiTests
    {
        private const bool overwriteTestResults = false;

        private const string BatchTeardownWarning = "Warning: Test teardown batch returned exit code 1";


        [ClassInitialize]
        public static void SetupVanillaVsExperimentalInstance(TestContext testContext)
        {
            VS.SetupVanillaVsExperimentalInstance();
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


        [TestMethod]
        [TestCategory("UI")]
        public void RunAllTests_GlobalAndSolutionSettings_BatchTeardownWarning()
        {
            try
            {
                VS.TestExplorer.RunAllTests();

                Assert.IsTrue(VS.GetOutput().Contains(BatchTeardownWarning));
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
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
                    VS.TestExplorer.SelectTestSettingsFile(VS.UserSettingsFile);

                    VS.TestExplorer.RunAllTests();

                    string output = VS.GetOutput();
                    Assert.IsTrue(output.Contains("--gtest_shuffle"));
                    Assert.IsTrue(output.Contains("--gtest_repeat=3"));
                    Assert.IsFalse(output.Contains(BatchTeardownWarning));
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

        [TestMethod]
        [TestCategory("UI")]
        public void RunAllTests__AllTestsAreRun()
        {
            try
            {
                VS.TestExplorer.RunAllTests();
                CheckResults();
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
            }
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_Crashing_AddPasses()
        {
            RunTest("Crashing.AddPasses");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_ParameterizedTests_Simple_0()
        {
            RunTest("ParameterizedTests.Simple/0");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_InstantiationName_ParameterizedTests_SimpleTraits_0()
        {
            RunTest("InstantiationName/ParameterizedTests.SimpleTraits/0");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_PointerParameterizedTests_CheckStringLength_0()
        {
            RunTest("PointerParameterizedTests.CheckStringLength/0");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_TypedTests_0_CanIterate()
        {
            RunTest("TypedTests/0.CanIterate");
        }

        [TestMethod]
        [TestCategory("UI")]
        public void RunSelectedTests_Arr_TypeParameterizedTests_1_CanDefeatMath()
        {
            RunTest("Arr/TypeParameterizedTests/1.CanDefeatMath");
        }

        [TestMethod, Ignore]
        [TestCategory("UI")]
        public void RunSelectedTests_MultipleTests()
        {
            RunTests(new[] { "Crashing.AddPasses", "ParameterizedTests.Simple/0",
                "InstantiationName/ParameterizedTests.SimpleTraits/0", "PointerParameterizedTests.CheckStringLength/0",
                "TypedTests/0.CanIterate", "Arr/TypeParameterizedTests/1.CanDefeatMath" });
        }


        private void RunTest(string displayName, [CallerMemberName] string testCaseName = null)
        {
            RunTests(new[] { displayName }, testCaseName);
        }

        private void RunTests(string[] displayNames, [CallerMemberName] string testCaseName = null)
        {
            try
            {
                VS.TestExplorer.RunSelectedTests(displayNames);
                CheckResults(testCaseName);
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
            }
        }


        private void CheckResults([CallerMemberName] string testCaseName = null)
        {
            string testResults = VS.TestExplorer.Parser.ParseTestResults().ToXML();

            string expectationFile = Path.Combine(VS.UiTestsDirectory, "UITestResults", this.GetType().Name + "__" + testCaseName + ".xml");
            string resultFile = Path.Combine(VS.UiTestsDirectory, "TestErrors", this.GetType().Name + "__" + testCaseName + ".xml");

            if (!File.Exists(expectationFile))
            {
                File.WriteAllText(expectationFile, testResults);
                Assert.Inconclusive("This is the first time this test runs.");
            }

            string expectedResult = File.ReadAllText(expectationFile);
            string msg;
            bool stringsAreEqual = AreEqual(expectedResult, testResults, out msg);
            if (!stringsAreEqual)
            {
#pragma warning disable CS0162 // Unreachable code (because overwriteTestResults is compile time constant)
                if (overwriteTestResults)
                {
                    File.WriteAllText(expectationFile, testResults);
                    Assert.Inconclusive("Test results changed and have been overwritten. Differences: " + msg);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(resultFile));
                    File.WriteAllText(resultFile, testResults);
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
                    messages.Add($"First difference at position {i}, "
                        + $"expected: {expectedResult[i]}, actual: {result[i]}, "
                        + $"context: '{GetContext(expectedResult, i)}' and '{GetContext(result, i)}'");
                    break;
                }
            }

            msg = string.Join("; ", messages);
            return areEqual;
        }

        private string GetContext(string result, int position, int contextLength = 40)
        {
            int leftContextLength = contextLength / 2;
            int rightContextLength = contextLength - leftContextLength;

            if (position - leftContextLength < 0)
            {
                int delta = leftContextLength - position;
                leftContextLength -= delta;
                rightContextLength += delta;
            }

            if (position + rightContextLength > result.Length)
            {
                int delta = position + rightContextLength - result.Length;
                rightContextLength -= delta;
            }

            return result.Substring(position - leftContextLength, leftContextLength + rightContextLength);
        }

    }

}