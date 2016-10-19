using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.Helpers;
using GoogleTestAdapter.Tests.Common.Model;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.TreeItems;
using TestStack.White.UIItems.WPFUIItems;

namespace GoogleTestAdapter.Tests.Common.UI
{
    public class Parser
    {
        private readonly Vsui _vsui;
        private readonly TestExplorer _testExplorer;

        public Parser(Vsui vsui, TestExplorer testExplorer)
        {
            _vsui = vsui;
            _testExplorer = testExplorer;
        }

        public TestRun ParseTestResults(bool includeNotRunTests = false)
        {
            _testExplorer.ScrollToTop();

            TestRun testResults = new TestRun();
            string tmp = _vsui.GetOutput().ReplaceIgnoreCase(Path.GetDirectoryName(_vsui.SolutionFile), "${SolutionDir}");
            tmp = Regex.Replace(tmp, "Found [0-9]+ tests in executable", "Found ${NrOfTests} tests in executable");
            testResults.testOutput = Regex.Replace(tmp, @"(========== Run test finished: [0-9]+ run )\([0-9:,\.]+\)( ==========)", "$1($${RunTime})$2");

            foreach (TreeNode testGroupNode in _testExplorer.GetTestCaseTree().Nodes)
            {
                if (!includeNotRunTests && testGroupNode.Text.StartsWith("Not Run Tests"))
                {
                    continue;
                }

                if (!_testExplorer.EnsureTestGroupNodeIsOnScreen(testGroupNode))
                {
                    continue;
                }

                testResults.Add(ParseTestGroup(testGroupNode));
            }
            return testResults;
        }

        private TestGroup ParseTestGroup(TreeNode testGroupNode)
        {
            TestGroup testGroup = new TestGroup(testGroupNode.Text);

            testGroupNode.Expand();
            for (int i = 0; i < testGroupNode.Nodes.Count; i++)
            {
                if (i < testGroupNode.Nodes.Count - 1)
                {
                    _testExplorer.EnsureTestCaseNodeIsOnScreen(testGroupNode.Nodes[i + 1]);
                }
                testGroup.Add(ParseTestCase(testGroupNode.Nodes[i]));
            }

            return testGroup;
        }

        private TestCase ParseTestCase(TreeNode testNode)
        {
            TestCase testResult = new TestCase();
            testNode.Get<Label>("TestListViewDisplayNameTextBlock").Click();
            testNode.Nodes.Count.Should().Be(0, "Test case tree node expected to have no children.");

            SearchCriteria isControlTypeLabel = SearchCriteria.ByControlType(typeof(Label), WindowsFramework.Wpf);
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (Label label in _testExplorer.GetTestExplorerDetailsPanel().GetMultiple(isControlTypeLabel))
            {
                if (label.IsOffScreen || string.IsNullOrWhiteSpace(label.Text))
                    continue;

                AddInfoFromDetailPane(testResult, label);
            }

            return testResult;
        }

        private void AddInfoFromDetailPane(TestCase testResult, Label label)
        {
            var id = label.AutomationElement.Current.AutomationId;
            switch (id)
            {
                case "detailPanelHeader":
                    var name = TestResources.NormalizePointerInfo(label.Text);
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
                    testResult.Result += TestResources.NormalizePointerInfo(label.Text);
                    break;
                case "errorMessageItem":
                    testResult.Error += label.Text.ReplaceIgnoreCase(Path.GetDirectoryName(_vsui.SolutionFile), "$(SolutionDir)");
                    break;
                case "hyperlinkText":
                    testResult.Stacktrace = label.Text.ReplaceIgnoreCase(Path.GetDirectoryName(_vsui.SolutionFile), "$(SolutionDir)");
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

    }
}