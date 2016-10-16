using System.Collections.Generic;
using System.Linq;
using TestStack.White;
using TestStack.White.InputDevices;
using TestStack.White.UIItems.TreeItems;
using TestStack.White.WindowsAPI;

namespace GoogleTestAdapter.Tests.Common.UI
{
    public class Selector
    {
        private readonly TestExplorer _testExplorer;
        public Selector(TestExplorer testExplorer)
        {
            _testExplorer = testExplorer;
        }

        public void SelectTestCases(params string[] displayNames)
        {
            List<TreeNode> testCaseNodes = FindTestCaseNodes(displayNames).ToList();

            if (testCaseNodes.Count > 0)
            {
                _testExplorer.ClickNode(testCaseNodes[0]);
            }

            if (testCaseNodes.Count > 1)
            {
                Keyboard.Instance.HoldKey(KeyboardInput.SpecialKeys.CONTROL);
                try
                {
                    for (int i = 1; i < testCaseNodes.Count; i++)
                    {
                        _testExplorer.ClickNode(testCaseNodes[i]);
                    }
                }
                finally
                {
                    Keyboard.Instance.LeaveKey(KeyboardInput.SpecialKeys.CONTROL);
                }
            }
        }


        private IEnumerable<TreeNode> FindTestCaseNodes(string[] displayNames)
        {
            List<string> namesToBeFound = displayNames.OrderBy(s => s).ToList();
            List<TreeNode> result = new List<TreeNode>();

            foreach (TreeNode testGroupNode in _testExplorer.GetTestCaseTree().Nodes)
            {
                if (namesToBeFound.Count == 0)
                {
                    break;
                }

                if (!_testExplorer.EnsureTestGroupNodeIsOnScreen(testGroupNode))
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
                    Debug.Details(_testExplorer.UiItem.AutomationElement));
            }

            return result;
        }

        private IDictionary<string, TreeNode> FindTestCaseNodes(TreeNode testGroupNode, List<string> displayNames)
        {
            IDictionary<string, TreeNode> result = new Dictionary<string, TreeNode>();
            testGroupNode.Expand();
            for (int i = 0; result.Count < displayNames.Count && i < testGroupNode.Nodes.Count; i++)
            {
                TreeNode node = testGroupNode.Nodes[i];
                _testExplorer.EnsureTestCaseNodeIsOnScreen(node);
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

    }
}