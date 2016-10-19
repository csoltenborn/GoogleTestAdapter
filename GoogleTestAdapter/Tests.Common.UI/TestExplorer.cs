using System;
using System.Windows.Automation;
using GoogleTestAdapter.Tests.Common.EndToEnd.VisualStudio;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Scrolling;
using TestStack.White.UIItems.TreeItems;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.UIItems.WPFUIItems;

namespace GoogleTestAdapter.Tests.Common.UI
{
    public class TestExplorer
    {
        private const double Tolerance = 0.01;
        private IUIItem _uiItem;
        private Window _mainWindow;
        private Vsui _vsui;
        private Parser _parser;

        internal IUIItem UiItem => _uiItem;

        public Parser Parser => _parser;

        public TestExplorer(Vsui vsui, Window mainWindow)
        {
            _mainWindow = mainWindow;
            _vsui = vsui;
            _parser = new Parser(vsui, vsui.TestExplorer);
        }

        public void SelectTestSettingsFile(string settingsFile)
        {
            _mainWindow.VsMenuBarMenuItems("Test", "Test Settings", "Select Test Settings File").Click();
            _vsui.FillFileDialog("Open Settings File", settingsFile);
        }

        public void UnselectTestSettingsFile()
        {
            SelectTestSettingsFile(_vsui.NoSettingsFile);
        }

        public void OpenTestExplorer()
        {
            if (_uiItem == null)
            {
                _mainWindow.VsMenuBarMenuItems("Test", "Windows", "Test Explorer").Click();
                _uiItem = _mainWindow.Get<UIItem>("TestWindowToolWindowControl");
            }

            ProgressBar delayIndicator = _uiItem.Get<ProgressBar>("delayIndicatorProgressBar");
            _mainWindow.WaitTill(() => delayIndicator.IsOffScreen);
        }

        public void RunAllTests()
        {
            RunTestsAndWait("All Tests");
        }

        public void RunSelectedTests(params string[] displayNames)
        {
            new Selector(this).SelectTestCases(displayNames);
            RunTestsAndWait("Selected Tests");
        }


        private void RunTestsAndWait(string whichTests)
        {
            _mainWindow.VsMenuBarMenuItems("Test", "Run", whichTests).Click();
            ProgressBar progressIndicator = _uiItem.Get<ProgressBar>("runProgressBar");
            _mainWindow.WaitTill(() => Math.Abs(progressIndicator.Value - progressIndicator.Maximum) < Tolerance);
        }

        internal void ScrollToTop()
        {
            GetTestCaseTree().ScrollBars.Vertical.SetToMinimum();
        }

        internal Tree GetTestCaseTree()
        {
            return _uiItem.Get<Tree>("TestsTreeView");
        }

        internal Panel GetTestExplorerDetailsPanel()
        {
            return _uiItem.Get<Panel>("LeftSelectionControl");
        }

        internal void ClickNode(TreeNode node)
        {
            EnsureTestCaseNodeIsOnScreen(node);
            node.Get<Label>("TestListViewDisplayNameTextBlock").Click();
        }

        internal bool EnsureTestGroupNodeIsOnScreen(TreeNode testGroupNode)
        {
            if (!SafeIsNodeOnScreen(testGroupNode) && testGroupNode.Nodes.Count > 0)
            {
                EnsureTestCaseNodeIsOnScreen(testGroupNode.Nodes[0]);
            }
            return SafeIsNodeOnScreen(testGroupNode);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        internal bool EnsureTestCaseNodeIsOnScreen(TreeNode node)
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

        private bool SafeIsNodeOnScreen(TreeNode node)
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

    }
}