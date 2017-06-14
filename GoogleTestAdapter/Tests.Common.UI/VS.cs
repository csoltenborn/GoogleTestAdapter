// This file has been modified by Microsoft on 6/2017.

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using GoogleTestAdapter.Tests.Common.EndToEnd.VisualStudio;
using TestStack.White;
using TestStack.White.Configuration;
using TestStack.White.Factory;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.UIItems.WPFUIItems;

namespace GoogleTestAdapter.Tests.Common.UI
{

    public class Vsui : Vs
    {
        private static readonly TimeSpan TimeOut = TimeSpan.FromMinutes(1);
        private static readonly int TimeOutInMs = (int)TimeOut.TotalMilliseconds;

        public TestExplorer TestExplorer => _testExplorer;

        internal readonly string SolutionFile;
        internal readonly string NoSettingsFile;

        private Application _application;
        private Window _mainWindow;
        private TestExplorer _testExplorer;

        public Vsui()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            // ReSharper disable once AssignNullToNotNullAttribute
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestAdapter\\VsPackage.Tests.*\\bin\\(Debug|Release)\\GoogleTestAdapter.Tests.Common.dll$");
            match.Success.Should().BeTrue();

            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            Path.Combine(basePath, @"GoogleTestAdapter\VsPackage\bin", debugOrRelease, @"GoogleTestAdapter.VsPackage.vsix");
            SolutionFile = Path.Combine(basePath, @"SampleTests\SampleTests.sln");
            NoSettingsFile = Path.Combine(basePath, @"SampleTests\No.runsettings");
        }

        public void OpenSolution()
        {
            _mainWindow.VsMenuBarMenuItems("File", "Open", "Project/Solution...").Click();
            FillFileDialog("Open Project", SolutionFile);
        }

        public void CloseSolution()
        {
            _mainWindow.VsMenuBarMenuItems("File", "Close Solution").Click();
        }

        public string GetOutput()
        {
            IUIItem outputWindow = _mainWindow.Get(SearchCriteria.ByText("Output").AndByClassName("GenericPane"), TimeSpan.FromSeconds(10));
            return outputWindow.Get<TextBox>("WpfTextView").Text;
        }


        internal void FillFileDialog(string dialogTitle, string file)
        {
            Window fileOpenDialog = _mainWindow.ModalWindow(dialogTitle);
            fileOpenDialog.Get<TextBox>(SearchCriteria.ByAutomationId("1148") /* File name: */).Text = file;
            fileOpenDialog.Get<Button>(SearchCriteria.ByAutomationId("1") /* Open */).Click();
        }

    }

}