using GoogleTestAdapterUiTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using TestStack.White;
using TestStack.White.Configuration;
using TestStack.White.Factory;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.UIItems.WPFUIItems;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace GoogleTestAdapterUiTests
{
    [TestClass]
    public class UiTests
    {
        private const bool keepDirtyInstanceInit = false;
        private const bool overwriteTestResults = false;

        static UiTests()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestExtension\\GoogleTestAdapterUiTests\\bin\\(Debug|Release)\\GoogleTestAdapterUiTests.dll$");
            Assert.IsTrue(match.Success);

            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            vsixPath = Path.Combine(basePath, @"GoogleTestExtension\GoogleTestAdapterVSIX\bin", debugOrRelease, @"GoogleTestAdapterVSIX.vsix");
            solution = Path.Combine(basePath, @"SampleGoogleTestTests\SampleGoogleTestTests.sln");
            uiTestsPath = Path.Combine(basePath, @"GoogleTestExtension\GoogleTestAdapterUiTests");
        }

        private static readonly string vsixPath;
        private static readonly string solution;
        private static readonly string uiTestsPath;
        private static bool keepDirtyVsInstance = keepDirtyInstanceInit;

        private VsExperimentalInstance visualStudioInstance;

        [TestInitialize]
        public void SetupVanillaVsExperimentalInstance()
        {
            try
            {
                visualStudioInstance = new VsExperimentalInstance(VsExperimentalInstance.Versions.VS2015, "GoogleTestAdapterUiTests");
                if (!keepDirtyVsInstance)
                {
                    keepDirtyVsInstance = AskToCleanIfExists(visualStudioInstance);
                }
                if (!keepDirtyVsInstance)
                {
                    visualStudioInstance.FirstTimeInitialization();
                    visualStudioInstance.InstallExtension(vsixPath);
                }
            }
            catch (AutomationException exception)
            {
                LogExceptionAndThrow(exception);
            }
        }

        [TestCleanup]
        public void CleanVsExperimentalInstance()
        {
            if (!keepDirtyVsInstance)
            {
                // wait for removal of locks on some files we want to delete
                // TODO: find more reliable method than using Sleep()
                Thread.Sleep(TimeSpan.FromSeconds(1));
                visualStudioInstance.Clean();
            }
            visualStudioInstance = null;
        }

        [TestMethod]
        public void MyFirstTest()
        {
            try
            {
                int timeOut = (int)TimeSpan.FromMinutes(3).TotalMilliseconds;
                using (Application application = visualStudioInstance.Launch())
                using (CoreAppXmlConfiguration.Instance.ApplyTemporarySetting(c => { c.BusyTimeout = c.FindWindowTimeout = timeOut; }))
                using (Window mainWindow = application.GetWindow(
                    SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                    InitializeOption.NoCache))
                {
                    // Open solution
                    mainWindow.VsMenuBarMenuItems("File", "Open", "Project/Solution...").Click();
                    Window fileOpenDialog = mainWindow.ModalWindow("Open Project");
                    fileOpenDialog.Get<TextBox>("File name:").Text = solution;
                    fileOpenDialog.Get<Button>("Open").Click();

                    // Open test explorer and wait for discovery
                    mainWindow.VsMenuBarMenuItems("Test", "Windows", "Test Explorer").Click();
                    IUIItem testExplorer = mainWindow.Get<UIItem>("TestWindowToolWindowControl");
                    ProgressBar delayIndicator = testExplorer.Get<ProgressBar>("delayIndicatorProgressBar");
                    mainWindow.WaitTill(() => delayIndicator.IsOffScreen);

                    // Run all tests and wait till finish (max 1 minute)
                    mainWindow.VsMenuBarMenuItems("Test", "Run", "All Tests").Click();
                    ProgressBar progressIndicator = testExplorer.Get<ProgressBar>("runProgressBar");
                    mainWindow.WaitTill(() => progressIndicator.Value == progressIndicator.Maximum, TimeSpan.FromMinutes(1));

                    // Check results
                    string solutionDir = Path.GetDirectoryName(solution);
                    IUIItem outputWindow = mainWindow.Get(SearchCriteria.ByText("Output"));
                    string testResults = new TestRunSerializer().ParseTestResults(solutionDir, testExplorer, outputWindow).ToXML();
                    CheckResults(testResults);
                }
            }
            catch(AutomationException exception)
            {
                LogExceptionAndThrow(exception);
            }
        }

        private void CheckResults(string result, [CallerMemberName] string testCaseName = null)
        {
            string expectationFile = Path.Combine(uiTestsPath, "TestResults", this.GetType().Name + "__" + testCaseName + "__expected.xml");
            string resultFile = Path.Combine(uiTestsPath, "TestErrors", this.GetType().Name + "__" + testCaseName + "__result.xml");

            if (!File.Exists(expectationFile))
            {
                File.WriteAllText(expectationFile, result);
                Assert.Inconclusive("This is the first time this test runs.");
            }

            string expectedResult = File.ReadAllText(expectationFile);
            if (result != expectedResult)
            {
                #pragma warning disable CS0162 // Unreachable code (because overwriteTestResults is compile time constant)
                if (overwriteTestResults)
                {
                    File.WriteAllText(expectationFile, result);
                    Directory.CreateDirectory(Path.GetDirectoryName(expectationFile));
                    Assert.Inconclusive("Test results changed and have been overwritten.");
                }
                else
                {
                    File.WriteAllText(resultFile, result);
                    Assert.Fail("Test result doesn't match expectation. Result written to: " + resultFile);
                }
                #pragma warning restore CS0162
            }
            else if (result == expectedResult && File.Exists(resultFile))
            {
                File.Delete(resultFile);
            }
        }

        private void LogExceptionAndThrow(AutomationException exception, [CallerMemberName] string testCaseName = null)
        {
            string debugDetailsFile = Path.Combine(uiTestsPath, "TestErrors", this.GetType().Name + "__" + testCaseName + "__DebugDetails.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(debugDetailsFile));
            File.WriteAllText(debugDetailsFile, exception.ToString() + "\r\n" + exception.StackTrace + "\r\n" + exception.DebugDetails);
            throw exception;
        }

        private static bool AskToCleanIfExists(VsExperimentalInstance visualStudioInstance)
        {
            bool keepDirtyInstance = false;
            if (visualStudioInstance.Exists())
            {
                var instanceExists = $"The experimental instance '{visualStudioInstance.VersionAndSuffix}' already exists.";
                var willReset = "\nShould it be deleted before going on with the tests?";

                MessageBoxResult result = MessageBoxWithTimeout.Show(instanceExists + willReset, "Warning!", MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        visualStudioInstance.Clean();
                        break;
                    case MessageBoxResult.No:
                        keepDirtyInstance = true;
                        break;
                    case MessageBoxResult.Cancel:
                        Assert.Fail(instanceExists + " Didn't get confirmation to reset experimental instance. Cancelling...");
                        break;
                }
            }
            return keepDirtyInstance;
        }
    }
}