using GoogleTestAdapterUiTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.TreeItems;
using TestStack.White.UIItems.WPFUIItems;
using System;
using System.Threading;
using System.Diagnostics;

namespace GoogleTestAdapterUiTests
{
    [XmlRoot("TestRun")]
    public class TestRun
    {
        [XmlElement("TestGroup")]
        public List<TestGroup> testGroups = new List<TestGroup>();

        [XmlElement("TestOutput")]
        public string testOutput;

        public void Add(TestGroup tg)
        {
            testGroups.Add(tg);
        }

        public string ToXML()
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer xmlSerializer = new XmlSerializer(GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, this, ns);
                return textWriter.ToString();
            }
        }
    }

    public class TestGroup
    {
        public string description;

        [XmlElement("TestCase")]
        public List<TestCase> testCases = new List<TestCase>();

        public TestGroup(string description)
        {
            this.description = description ?? string.Empty;
        }
        private TestGroup() { } // only to make XmlSerializer happy

        public void Add(TestCase tr) { testCases.Add(tr); }
    }

    public class TestCase
    {
        public string Name               = string.Empty;
        public string FullyQualifiedName = string.Empty;
        public string Result             = string.Empty;
        public string Source             = string.Empty;
        public string Error              = string.Empty;
        public string Unexpected         = string.Empty;
        public bool ShouldSerializeFullyQualifiedName() { return !string.IsNullOrWhiteSpace(FullyQualifiedName); }
        public bool ShouldSerializeError()              { return !string.IsNullOrWhiteSpace(Error); }
        public bool ShouldSerializeUnexpected()         { return !string.IsNullOrWhiteSpace(Unexpected); }
    }

    public class TestRunSerializer
    {
        public TestRun ParseTestResults(string solutionDir, IUIItem testExplorer, IUIItem outputWindow)
        {
            this.solutionDir = solutionDir;
            this.detailsPane = testExplorer.Get<Panel>("LeftSelectionControl");

            TestRun testResults = new TestRun();
            string tmp = outputWindow.Get<TextBox>("WpfTextView").Text.ReplaceIgnoreCase(solutionDir, "${SolutionDir}");
            testResults.testOutput = Regex.Replace(tmp, @"(========== Run test finished: [0-9]+ run )\([0-9:,]+\)( ==========)", "$1($${RunTime})$2");

            Tree testTree = testExplorer.Get<Tree>("TestsTreeView");
            foreach (TreeNode testGroupNode in testTree.Nodes)
            {
                if (testGroupNode.IsOffScreen)
                    continue;

                testResults.Add(ParseTestGroup(testGroupNode));
            }
            return testResults;
        }

        private string solutionDir;
        private Panel detailsPane;

        private TestGroup ParseTestGroup(TreeNode testGroupNode)
        {
            TestGroup testGroup = new TestGroup(testGroupNode.Text);

            testGroupNode.Expand();
            for (int i = 0; i < testGroupNode.Nodes.Count; i++)
            {
                if (i < testGroupNode.Nodes.Count - 1)
                {
                    EnsureNodeIsOnScreen(testGroupNode.Nodes[i + 1]);
                }
                testGroup.Add(ParseTestCase(testGroupNode.Nodes[i]));
            }

            return testGroup;
        }

        private TestCase ParseTestCase(TreeNode testNode)
        {
            TestCase testResult = new TestCase();
            testNode.Get<Label>("TestListViewDisplayNameTextBlock").Click();
            Assert.AreEqual(testNode.Nodes.Count, 0, "Test case tree node expected to have no children.");

            SearchCriteria isControlTypeLabel = SearchCriteria.ByControlType(typeof(Label), WindowsFramework.Wpf);
            foreach (Label label in detailsPane.GetMultiple(isControlTypeLabel))
            {
                if (label.IsOffScreen || string.IsNullOrWhiteSpace(label.Text))
                    continue;

                AddInfoFromDetailPane(testResult, label);
            }

            return testResult;
        }

        private void EnsureNodeIsOnScreen(TreeNode node)
        {
            if (node.IsOffScreen)
            {
                node.Get<Label>("TestListViewDisplayNameTextBlock").Focus();
                node.Get<Label>("TestListViewDisplayNameTextBlock").Click();
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
        }

        private void AddInfoFromDetailPane(TestCase testResult, Label label)
        {
            var id = label.AutomationElement.Current.AutomationId;
            switch (id)
            {
                case "detailPanelHeader":
                    var name = Regex.Replace(label.Text, "([0-9A-F]{8}){1,2} pointing to", "${MemoryLocation} pointing to");
                    testResult.Name += name;
                    if(label.Text != label.HelpText)
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
                    testResult.Error += label.Text.ReplaceIgnoreCase(solutionDir, "$(SolutionDir)");
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
    }
}
