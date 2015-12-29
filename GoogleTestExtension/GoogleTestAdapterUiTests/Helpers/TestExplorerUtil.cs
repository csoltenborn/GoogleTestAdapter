using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestStack.White;
using TestStack.White.InputDevices;
using TestStack.White.UIItems;
using TestStack.White.UIItems.TreeItems;
using TestStack.White.UIItems.WPFUIItems;
using TestStack.White.WindowsAPI;

namespace GoogleTestAdapterUiTests.Helpers
{

    public class TestExplorerUtil
    {
        private const int WaitingTimeInMs = 500;


        private IUIItem TestExplorer { get; }

        public TestExplorerUtil(IUIItem testExplorer)
        {
            TestExplorer = testExplorer;
        }

        public void SelectTestCases(params string[] displayNames)
        {
            List<TreeNode> testCaseNodes = GetTestCaseNodes(displayNames).ToList();

            if (testCaseNodes.Count > 0)
            {
                testCaseNodes[0].Get<Label>("TestListViewDisplayNameTextBlock").Focus();
                testCaseNodes[0].Get<Label>("TestListViewDisplayNameTextBlock").Click();
            }

            Keyboard.Instance.HoldKey(KeyboardInput.SpecialKeys.CONTROL);
            for (int i = 1; i < testCaseNodes.Count; i++)
            {
                testCaseNodes[i].Get<Label>("TestListViewDisplayNameTextBlock").Focus();
                testCaseNodes[i].Get<Label>("TestListViewDisplayNameTextBlock").Click();
            }
            Keyboard.Instance.LeaveKey(KeyboardInput.SpecialKeys.CONTROL);
        }

        private IEnumerable<TreeNode> GetTestCaseNodes(params string[] displayNames)
        {
            List<TreeNode> result = new List<TreeNode>();
            foreach (string displayName in displayNames)
            {
                bool found = false;
                Tree testTree = TestExplorer.Get<Tree>("TestsTreeView");
                foreach (TreeNode testGroupNode in testTree.Nodes)
                {
                    if (testGroupNode.IsOffScreen)
                        continue;

                    TreeNode node = ParseTestGroup(testGroupNode, displayName);
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
                        Debug.Details(TestExplorer.AutomationElement));
                }
            }

            return result;
        }

        private TreeNode ParseTestGroup(TreeNode testGroupNode, string displayName)
        {
            displayName += ":NotRun";

            testGroupNode.Expand();
            for (int i = 0; i < testGroupNode.Nodes.Count; i++)
            {
                if (i < testGroupNode.Nodes.Count - 1)
                {
                    EnsureNodeIsOnScreen(testGroupNode.Nodes[i + 1]);
                }

                TreeNode node = testGroupNode.Nodes[i];
                string name = node.Text;
                if (displayName.Equals(name))
                {
                    return node;
                }
            }

            return null;
        }

        private void EnsureNodeIsOnScreen(TreeNode node)
        {
            if (node.IsOffScreen)
            {
                node.Get<Label>("TestListViewDisplayNameTextBlock").Focus();
                node.Get<Label>("TestListViewDisplayNameTextBlock").Click();
                Thread.Sleep(TimeSpan.FromMilliseconds(WaitingTimeInMs));
            }
        }

    }

}