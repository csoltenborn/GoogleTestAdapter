using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
using FluentAssertions;
using GoogleTestAdapter;
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
using TestStack.White.UIItems.Scrolling;
using GoogleTestAdapterUiTests.Helpers;
using GoogleTestAdapterUiTests.Model;

namespace GoogleTestAdapterUiTests
{
    public static class VS
    {

        public static class TestExplorer
        {
            private const double Tolerance = 0.01;

            public static class Parser
            {

                public static TestRun ParseTestResults(bool includeNotRunTests = false)
                {
                    ScrollToTop();

                    TestRun testResults = new TestRun();
                    string tmp = GetOutput().ReplaceIgnoreCase(Path.GetDirectoryName(SolutionFile), "${SolutionDir}");
                    tmp = Regex.Replace(tmp, "Found [0-9]+ tests in executable", "Found ${NrOfTests} tests in executable");
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
                    testNode.Nodes.Count.Should().Be(0, "Test case tree node expected to have no children.");

                    SearchCriteria isControlTypeLabel = SearchCriteria.ByControlType(typeof(Label), WindowsFramework.Wpf);
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (Label label in GetTestExplorerDetailsPanel().GetMultiple(isControlTypeLabel))
                    {
                        if (label.IsOffScreen || string.IsNullOrWhiteSpace(label.Text))
                            continue;

                        AddInfoFromDetailPane(testResult, label);
                    }

                    return testResult;
                }

                public static string NormalizePointerInfo(string text)
                {
                    return Regex.Replace(text, "([0-9A-F]{8}){1,2} pointing to", "${MemoryLocation} pointing to");
                }

                private static void AddInfoFromDetailPane(TestCase testResult, Label label)
                {
                    var id = label.AutomationElement.Current.AutomationId;
                    switch (id)
                    {
                        case "detailPanelHeader":
                            var name = NormalizePointerInfo(label.Text);
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
                            testResult.Result += NormalizePointerInfo(label.Text);
                            break;
                        case "errorMessageItem":
                            testResult.Error += label.Text.ReplaceIgnoreCase(Path.GetDirectoryName(SolutionFile), "$(SolutionDir)");
                            break;
                        case "hyperlinkText":
                            testResult.Stacktrace = label.Text.ReplaceIgnoreCase(Path.GetDirectoryName(SolutionFile), "$(SolutionDir)");
                            break;
                        case "sourceTextHeader":
                        case "noSourceAvailableToolTip":
                        case "elapsedTimeText":
                        case "Stacktraceheader":
                        case "StackFramePanel":
                            // ignore
                            break;
                        default:
                            testResult.Unexpected += $"{id}={label.Text} ";
                            break;
                    }
                }

            } // class Parser

            public static class Selector
            {

                public static void SelectTestCases(params string[] displayNames)
                {
                    List<TreeNode> testCaseNodes = FindTestCaseNodes(displayNames).ToList();

                    if (testCaseNodes.Count > 0)
                    {
                        ClickNode(testCaseNodes[0]);
                    }

                    if (testCaseNodes.Count > 1)
                    {
                        Keyboard.Instance.HoldKey(KeyboardInput.SpecialKeys.CONTROL);
                        try
                        {
                            for (int i = 1; i < testCaseNodes.Count; i++)
                            {
                                ClickNode(testCaseNodes[i]);
                            }
                        }
                        finally
                        {
                            Keyboard.Instance.LeaveKey(KeyboardInput.SpecialKeys.CONTROL);
                        }
                    }
                }


                private static IEnumerable<TreeNode> FindTestCaseNodes(string[] displayNames)
                {
                    List<string> namesToBeFound = displayNames.OrderBy(s => s).ToList();
                    List<TreeNode> result = new List<TreeNode>();

                    foreach (TreeNode testGroupNode in GetTestCaseTree().Nodes)
                    {
                        if (namesToBeFound.Count == 0)
                        {
                            break;
                        }

                        if (!EnsureTestGroupNodeIsOnScreen(testGroupNode))
                        {
                            continue;
                        }

                        IDictionary<string, TreeNode> foundTestCases = FindTestCaseNodes(testGroupNode, namesToBeFound);
                        namesToBeFound.RemoveAll(s => foundTestCases.ContainsKey(s));
                        result.AddRange(foundTestCases.Values);
                    }

                    if (namesToBeFound.Count > 0)
                    {
                        string missingTestCases = string.Join(", ", namesToBeFound);
                        throw new AutomationException(
                            $"Could not find test cases {missingTestCases} in test explorer",
                            Debug.Details(_testExplorer.AutomationElement));
                    }

                    return result;
                }

                private static IDictionary<string, TreeNode> FindTestCaseNodes(TreeNode testGroupNode, List<string> displayNames)
                {
                    IDictionary<string, TreeNode> result = new Dictionary<string, TreeNode>();
                    testGroupNode.Expand();
                    for (int i = 0; result.Count < displayNames.Count && i < testGroupNode.Nodes.Count; i++)
                    {
                        TreeNode node = testGroupNode.Nodes[i];
                        EnsureTestCaseNodeIsOnScreen(node);
                        foreach (string displayName in displayNames)
                        {
                            if (node.Text.StartsWith(displayName))
                            {
                                result.Add(displayName, node);
                            }
                        }
                    }
                    return result;
                }

            } // class Selector

            public static void SelectTestSettingsFile(string settingsFile)
            {
                _mainWindow.VsMenuBarMenuItems("Test", "Test Settings", "Select Test Settings File").Click();
                FillFileDialog("Open Settings File", settingsFile);
            }

            public static void UnselectTestSettingsFile()
            {
                SelectTestSettingsFile(NoSettingsFile);
            }

            public static void OpenTestExplorer()
            {
                if (_testExplorer == null)
                {
                    _mainWindow.VsMenuBarMenuItems("Test", "Windows", "Test Explorer").Click();
                    _testExplorer = _mainWindow.Get<UIItem>("TestWindowToolWindowControl");
                }

                ProgressBar delayIndicator = _testExplorer.Get<ProgressBar>("delayIndicatorProgressBar");
                _mainWindow.WaitTill(() => delayIndicator.IsOffScreen);
            }

            public static void RunAllTests()
            {
                RunTestsAndWait("All Tests");
            }

            public static void RunSelectedTests(params string[] displayNames)
            {
                Selector.SelectTestCases(displayNames);
                RunTestsAndWait("Selected Tests");
            }


            private static void RunTestsAndWait(string whichTests)
            {
                _mainWindow.VsMenuBarMenuItems("Test", "Run", whichTests).Click();
                ProgressBar progressIndicator = _testExplorer.Get<ProgressBar>("runProgressBar");
                _mainWindow.WaitTill(() => Math.Abs(progressIndicator.Value - progressIndicator.Maximum) < Tolerance);
            }

            private static void ScrollToTop()
            {
                GetTestCaseTree().ScrollBars.Vertical.SetToMinimum();
            }

            private static Tree GetTestCaseTree()
            {
                return _testExplorer.Get<Tree>("TestsTreeView");
            }

            private static Panel GetTestExplorerDetailsPanel()
            {
                return _testExplorer.Get<Panel>("LeftSelectionControl");
            }

            private static void ClickNode(TreeNode node)
            {
                EnsureTestCaseNodeIsOnScreen(node);
                node.Get<Label>("TestListViewDisplayNameTextBlock").Click();
            }

            private static bool EnsureTestGroupNodeIsOnScreen(TreeNode testGroupNode)
            {
                if (!SafeIsNodeOnScreen(testGroupNode) && testGroupNode.Nodes.Count > 0)
                {
                    EnsureTestCaseNodeIsOnScreen(testGroupNode.Nodes[0]);
                }
                return SafeIsNodeOnScreen(testGroupNode);
            }

            // ReSharper disable once UnusedMethodReturnValue.Local
            private static bool EnsureTestCaseNodeIsOnScreen(TreeNode node)
            {
                if (SafeIsNodeOnScreen(node))
                {
                    return true;
                }

                Tree tree = GetTestCaseTree();
                IVScrollBar scrollBar = tree.ScrollBars.Vertical;
                double initialValue = scrollBar.Value;
                while (scrollBar.Value < scrollBar.MaximumValue && !SafeIsNodeOnScreen(node))
                {
                    scrollBar.ScrollDownLarge();
                }

                if (!SafeIsNodeOnScreen(node))
                {
                    scrollBar.SetToMinimum();
                    while (scrollBar.Value < initialValue && !SafeIsNodeOnScreen(node))
                    {
                        scrollBar.ScrollDownLarge();
                    }
                }

                return SafeIsNodeOnScreen(node);
            }

            private static bool SafeIsNodeOnScreen(TreeNode node)
            {
                try
                {
                    return node.Visible;
                }
                catch (ElementNotAvailableException)
                {
                    return false;
                }
            }

        } // class TestExplorer


        private const bool KeepDirtyInstanceInit = false;

        private static readonly TimeSpan TimeOut = TimeSpan.FromMinutes(1);
        private static readonly int TimeOutInMs = (int)TimeOut.TotalMilliseconds;


        public static string UiTestsDirectory { get; }
        public static string UserSettingsFile { get; }

        private static readonly string VsixPath;
        private static readonly string SolutionFile;
        private static readonly string NoSettingsFile;

        private static bool _keepDirtyVsInstance = KeepDirtyInstanceInit;
        private static bool? _installIntoProductiveVs;

        private static VsExperimentalInstance _visualStudioInstance;
        private static Application _application;
        private static Window _mainWindow;
        private static IUIItem _testExplorer;

        static VS()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestAdapter\\VsPackage.Tests.*\\bin\\(Debug|Release)\\GoogleTestAdapter.VsPackage.Tests.*.dll$");
            match.Success.Should().BeTrue();

            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            VsixPath = Path.Combine(basePath, @"GoogleTestAdapter\VsPackage\bin", debugOrRelease, @"GoogleTestAdapter.VsPackage.vsix");
            SolutionFile = Path.Combine(basePath, @"SampleTests\SampleTests.sln");
            UiTestsDirectory = Path.Combine(basePath, @"GoogleTestAdapter\VsPackage.Tests");
            UserSettingsFile = Path.Combine(basePath, @"SampleTests\NonDeterministic.runsettings");
            NoSettingsFile = Path.Combine(basePath, @"SampleTests\No.runsettings");
        }

        public static void SetupVanillaVsExperimentalInstance(string suffix)
        {
            AskIfNotOnBuildServerAndProductiveVs(suffix);

            try
            {
                _visualStudioInstance = new VsExperimentalInstance(VsExperimentalInstance.Versions.VS2015, suffix);
                if (string.IsNullOrEmpty(suffix))
                {
                    _keepDirtyVsInstance = true;
                    _visualStudioInstance.InstallExtension(VsixPath);
                }
                else
                {
                    if (!_keepDirtyVsInstance)
                    {
                        _keepDirtyVsInstance = AskToCleanIfExists();
                    }
                    if (!_keepDirtyVsInstance)
                    {
                        _visualStudioInstance.FirstTimeInitialization();
                        _visualStudioInstance.InstallExtension(VsixPath);
                    }
                }
            }
            catch (AutomationException exception)
            {
                exception.LogAndThrow();
            }
        }

        public static void LaunchVsExperimentalInstance()
        {
            _application = _visualStudioInstance.Launch();
            CoreAppXmlConfiguration.Instance.ApplyTemporarySetting(
                c => { c.BusyTimeout = c.FindWindowTimeout = TimeOutInMs; });

            _mainWindow = _application.GetWindow(
                SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                InitializeOption.NoCache);
        }

        public static void CleanVsExperimentalInstance()
        {
            _mainWindow?.Dispose();
            _application?.Dispose();

            _testExplorer = null;
            _mainWindow = null;
            _application = null;

            if (!_keepDirtyVsInstance)
            {
                // wait for removal of locks on some files we want to delete
                // TODO: find more reliable method than using Sleep()
                Thread.Sleep(TimeSpan.FromSeconds(1));
                _visualStudioInstance.Clean();
            }
            _visualStudioInstance = null;
        }


        public static void OpenSolution()
        {
            _mainWindow.VsMenuBarMenuItems("File", "Open", "Project/Solution...").Click();
            FillFileDialog("Open Project", SolutionFile);
        }

        public static void CloseSolution()
        {
            _mainWindow.VsMenuBarMenuItems("File", "Close Solution").Click();
        }

        public static string GetOutput()
        {
            IUIItem outputWindow = _mainWindow.Get(SearchCriteria.ByText("Output").AndByClassName("GenericPane"), TimeSpan.FromSeconds(10));
            return outputWindow.Get<TextBox>("WpfTextView").Text;
        }


        private static void FillFileDialog(string dialogTitle, string file)
        {
            Window fileOpenDialog = _mainWindow.ModalWindow(dialogTitle);
            fileOpenDialog.Get<TextBox>(SearchCriteria.ByAutomationId("1148") /* File name: */).Text = file;
            fileOpenDialog.Get<Button>(SearchCriteria.ByAutomationId("1") /* Open */).Click();
        }

        private static void AskIfNotOnBuildServerAndProductiveVs(string suffix)
        {
            if (string.IsNullOrEmpty(suffix)
                && _installIntoProductiveVs == null
                && !CiSupport.IsRunningOnBuildServer)
            {
                MessageBoxResult result = MessageBoxWithTimeout.Show(
                    "Really launch tests? This will delete a potentially installed GoogleTestAdapter extension "
                    + "from your productive VisualStudio instance and install this build instead!",
                    "Warning!", MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        _installIntoProductiveVs = true;
                        break;
                    default:
                        _installIntoProductiveVs = false;
                        break;
                }
            }

            if (_installIntoProductiveVs.HasValue && !_installIntoProductiveVs.Value)
                Assert.Inconclusive("Didn't get confirmation to execute tests. Cancelling...");
        }

        private static bool AskToCleanIfExists()
        {
            bool keepDirtyInstance = false;
            if (_visualStudioInstance.Exists() && !CiSupport.IsRunningOnBuildServer)
            {
                var instanceExists = $"The experimental instance '{_visualStudioInstance.VersionAndSuffix}' already exists.";
                var willReset = "\nShould it be deleted before going on with the tests?";

                MessageBoxResult result = MessageBoxWithTimeout.Show(instanceExists + willReset, "Warning!", MessageBoxButton.YesNoCancel, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        _visualStudioInstance.Clean();
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