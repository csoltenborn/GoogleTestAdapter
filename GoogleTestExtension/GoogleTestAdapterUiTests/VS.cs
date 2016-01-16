using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestStack.White;
using TestStack.White.Configuration;
using TestStack.White.Factory;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.UIItems.WPFUIItems;
using TestStack.White.UIItems.TreeItems;
using TestStack.White.InputDevices;
using TestStack.White.WindowsAPI;
using GoogleTestAdapterUiTests.Helpers;
using GoogleTestAdapterUiTests.Model;

namespace GoogleTestAdapterUiTests
{
    internal static class VS
    {

        internal static class TestExplorer
        {

            internal static class Parser
            {

                internal static TestRun ParseTestResults(bool includeNotRunTests = false)
                {
                    TestRun testResults = new TestRun();
                    string tmp = GetOutput().ReplaceIgnoreCase(Path.GetDirectoryName(solutionFile), "${SolutionDir}");
                    testResults.testOutput = Regex.Replace(tmp, @"(========== Run test finished: [0-9]+ run )\([0-9:,\.]+\)( ==========)", "$1($${RunTime})$2");

                    foreach (TreeNode testGroupNode in GetTestCaseTree().Nodes)
                    {
                        if (!includeNotRunTests && testGroupNode.Text.StartsWith("Not Run Tests"))
                        {
                            continue;
                        }

                        if (!EnsureTestGroupNodeIsOnScreen(testGroupNode))
                        {
                            continue;
                        }

                        testResults.Add(ParseTestGroup(testGroupNode));
                    }
                    return testResults;
                }

                private static TestGroup ParseTestGroup(TreeNode testGroupNode)
                {
                    TestGroup testGroup = new TestGroup(testGroupNode.Text);

                    testGroupNode.Expand();
                    for (int i = 0; i < testGroupNode.Nodes.Count; i++)
                    {
                        if (i < testGroupNode.Nodes.Count - 1)
                        {
                            EnsureTestCaseNodeIsOnScreen(testGroupNode.Nodes[i + 1]);
                        }
                        testGroup.Add(ParseTestCase(testGroupNode.Nodes[i]));
                    }

                    return testGroup;
                }

                private static TestCase ParseTestCase(TreeNode testNode)
                {
                    TestCase testResult = new TestCase();
                    testNode.Get<Label>("TestListViewDisplayNameTextBlock").Click();
                    Assert.AreEqual(testNode.Nodes.Count, 0, "Test case tree node expected to have no children.");

                    SearchCriteria isControlTypeLabel = SearchCriteria.ByControlType(typeof(Label), WindowsFramework.Wpf);
                    foreach (Label label in GetTestExplorerDetailsPanel().GetMultiple(isControlTypeLabel))
                    {
                        if (label.IsOffScreen || string.IsNullOrWhiteSpace(label.Text))
                            continue;

                        AddInfoFromDetailPane(testResult, label);
                    }

                    return testResult;
                }

                private static void AddInfoFromDetailPane(TestCase testResult, Label label)
                {
                    var id = label.AutomationElement.Current.AutomationId;
                    switch (id)
                    {
                        case "detailPanelHeader":
                            var name = Regex.Replace(label.Text, "([0-9A-F]{8}){1,2} pointing to", "${MemoryLocation} pointing to");
                            testResult.Name += name;
                            if (label.Text != label.HelpText)
                                testResult.FullyQualifiedName += label.HelpText;
                            break;
                        case "hasSourceToolTip":
                            testResult.Source += label.Text;
                            break;
                        case "testResultSummaryText Failed":
                        case "testResultSummaryText Skipped":
                        case "testResultSummaryText Passed":
                            testResult.Result += label.Text;
                            break;
                        case "errorMessageItem":
                            testResult.Error += label.Text.ReplaceIgnoreCase(Path.GetDirectoryName(solutionFile), "$(SolutionDir)");
                            break;
                        case "sourceTextHeader":
                        case "noSourceAvailableToolTip":
                        case "elapsedTimeText":
                            // ignore
                            break;
                        default:
                            testResult.Unexpected += $"{id}={label.Text} ";
                            break;
                    }
                }

            } // class Parser

            internal static class Selector
            {

                internal static void SelectTestCases(params string[] displayNames)
                {
                    List<TreeNode> testCaseNodes = FindTestCaseNodes(displayNames).ToList();

                    if (testCaseNodes.Count > 0)
                    {
                        ClickNode(testCaseNodes[0]);
                    }

                    if (testCaseNodes.Count > 1)
                    {
                        Keyboard.Instance.HoldKey(KeyboardInput.SpecialKeys.CONTROL);
                        for (int i = 1; i < testCaseNodes.Count; i++)
                        {
                            ClickNode(testCaseNodes[i]);
                        }
                        Keyboard.Instance.LeaveKey(KeyboardInput.SpecialKeys.CONTROL);
                    }
                }


                private static IEnumerable<TreeNode> FindTestCaseNodes(string[] displayNames)
                {
                    List<TreeNode> result = new List<TreeNode>();
                    foreach (string displayName in displayNames)
                    {
                        bool found = false;
                        foreach (TreeNode testGroupNode in GetTestCaseTree().Nodes)
                        {
                            if (!EnsureTestGroupNodeIsOnScreen(testGroupNode))
                            {
                                continue;
                            }

                            TreeNode node = FindTestCaseNode(testGroupNode, displayName);
                            if (node != null)
                            {
                                result.Add(node);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            throw new AutomationException(
                                $"Could not find test case {displayName} in test explorer",
                                Debug.Details(testExplorer.AutomationElement));
                        }
                    }

                    return result;
                }

                private static TreeNode FindTestCaseNode(TreeNode testGroupNode, string displayName)
                {
                    testGroupNode.Expand();
                    for (int i = 0; i < testGroupNode.Nodes.Count; i++)
                    {
                        if (i < testGroupNode.Nodes.Count - 1)
                        {
                            EnsureTestCaseNodeIsOnScreen(testGroupNode.Nodes[i + 1]);
                        }

                        TreeNode node = testGroupNode.Nodes[i];
                        if (node.Text.StartsWith(displayName))
                        {
                            return node;
                        }
                    }

                    return null;
                }

            } // class Selector

            internal static void SelectTestSettingsFile(string settingsFile)
            {
                mainWindow.VsMenuBarMenuItems("Test", "Test Settings", "Select Test Settings File").Click();
                FillFileDialog("Open Settings File", settingsFile);
            }

            internal static void UnselectTestSettingsFile()
            {
                SelectTestSettingsFile(noSettingsFile);
            }

            internal static void OpenTestExplorer()
            {
                if (testExplorer == null)
                {
                    mainWindow.VsMenuBarMenuItems("Test", "Windows", "Test Explorer").Click();
                    testExplorer = mainWindow.Get<UIItem>("TestWindowToolWindowControl");
                }

                ProgressBar delayIndicator = testExplorer.Get<ProgressBar>("delayIndicatorProgressBar");
                mainWindow.WaitTill(() => delayIndicator.IsOffScreen);
            }

            internal static void RunAllTests()
            {
                RunTestsAndWait("All Tests");
            }

            internal static void RunSelectedTests(params string[] displayNames)
            {
                Selector.SelectTestCases(displayNames);
                RunTestsAndWait("Selected Tests");
            }


            private static void RunTestsAndWait(string whichTests)
            {
                mainWindow.VsMenuBarMenuItems("Test", "Run", whichTests).Click();
                ProgressBar progressIndicator = testExplorer.Get<ProgressBar>("runProgressBar");
                mainWindow.WaitTill(() => progressIndicator.Value == progressIndicator.Maximum);
            }

            private static Tree GetTestCaseTree()
            {
                return testExplorer.Get<Tree>("TestsTreeView");
            }

            private static Panel GetTestExplorerDetailsPanel()
            {
                return testExplorer.Get<Panel>("LeftSelectionControl");
            }

            private static void ClickNode(TreeNode node)
            {
                node.Get<Label>("TestListViewDisplayNameTextBlock").Click();
            }

            private static bool EnsureTestGroupNodeIsOnScreen(TreeNode testGroupNode)
            {
                if (testGroupNode.IsOffScreen && testGroupNode.Nodes.Count > 0)
                {
                    EnsureTestCaseNodeIsOnScreen(testGroupNode.Nodes[0]);
                }
                return !testGroupNode.IsOffScreen;
            }

            private static bool EnsureTestCaseNodeIsOnScreen(TreeNode node)
            {
                if (node.IsOffScreen)
                {
                    ClickNode(node);
                    Thread.Sleep(TimeSpan.FromMilliseconds(WaitingTimeInMs));
                }
                return !node.IsOffScreen;
            }

        } // class TestExplorer


        private const bool keepDirtyInstanceInit = false;

        private static readonly TimeSpan TimeOut = TimeSpan.FromMinutes(1);
        private static readonly int TimeOutInMs = (int)TimeOut.TotalMilliseconds;
        private const int WaitingTimeInMs = 500;


        internal static string UiTestsDirectory { get; }
        internal static string UserSettingsFile { get; }

        private static readonly string vsixPath;
        private static readonly string solutionFile;
        private static readonly string noSettingsFile;

        private static bool keepDirtyVsInstance = keepDirtyInstanceInit;

        private static VsExperimentalInstance visualStudioInstance;
        private static Application application;
        private static Window mainWindow;
        private static IUIItem testExplorer;

        static VS()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestExtension\\GoogleTestAdapterUiTests\\bin\\(Debug|Release)\\GoogleTestAdapterUiTests.dll$");
            Assert.IsTrue(match.Success);

            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            vsixPath = Path.Combine(basePath, @"GoogleTestExtension\GoogleTestAdapterVSIX\bin", debugOrRelease, @"GoogleTestAdapterVSIX.vsix");
            solutionFile = Path.Combine(basePath, @"SampleGoogleTestTests\SampleGoogleTestTests.sln");
            UiTestsDirectory = Path.Combine(basePath, @"GoogleTestExtension\GoogleTestAdapterUiTests");
            UserSettingsFile = Path.Combine(basePath, @"SampleGoogleTestTests\NonDeterministic.runsettings");
            noSettingsFile = Path.Combine(basePath, @"SampleGoogleTestTests\No.runsettings");
        }

        internal static void SetupVanillaVsExperimentalInstance()
        {
            string solutionDir = Path.GetDirectoryName(solutionFile);
            string vsDir = Path.Combine(solutionDir, ".vs");
            if (Directory.Exists(vsDir))
            {
                Directory.Delete(vsDir, true);
            }

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
                exception.LogAndThrow();
            }

            application = visualStudioInstance.Launch();
            CoreAppXmlConfiguration.Instance.ApplyTemporarySetting(
                c => { c.BusyTimeout = c.FindWindowTimeout = TimeOutInMs; });

            mainWindow = application.GetWindow(
                SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                InitializeOption.NoCache);
        }

        internal static void CleanVsExperimentalInstance()
        {
            mainWindow.Dispose();
            application.Dispose();

            testExplorer = null;
            mainWindow = null;
            application = null;

            if (!keepDirtyVsInstance)
            {
                // wait for removal of locks on some files we want to delete
                // TODO: find more reliable method than using Sleep()
                Thread.Sleep(TimeSpan.FromSeconds(1));
                visualStudioInstance.Clean();
            }
            visualStudioInstance = null;
        }


        internal static void OpenSolution()
        {
            mainWindow.VsMenuBarMenuItems("File", "Open", "Project/Solution...").Click();
            FillFileDialog("Open Project", solutionFile);
        }

        internal static void CloseSolution()
        {
            mainWindow.VsMenuBarMenuItems("File", "Close Solution").Click();
        }

        internal static string GetOutput()
        {
            IUIItem outputWindow = VS.mainWindow.Get(SearchCriteria.ByText("Output").AndByClassName("GenericPane"), TimeSpan.FromSeconds(10));
            return outputWindow.Get<TextBox>("WpfTextView").Text;
        }


        private static void FillFileDialog(string dialogTitle, string file)
        {
            Window fileOpenDialog = mainWindow.ModalWindow(dialogTitle);
            fileOpenDialog.Get<TextBox>(SearchCriteria.ByAutomationId("1148") /* File name: */).Text = file;
            fileOpenDialog.Get<Button>(SearchCriteria.ByAutomationId("1") /* Open */).Click();
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