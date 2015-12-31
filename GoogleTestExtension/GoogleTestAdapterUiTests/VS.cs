using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using GoogleTestAdapterUiTests.Helpers;

namespace GoogleTestAdapterUiTests
{
    internal static class VS
    {
        private const bool keepDirtyInstanceInit = false;

        private static readonly int TimeOut = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        internal static Window MainWindow { get; private set; }

        internal static readonly string SolutionDirectory;
        internal static readonly string UiTestsDirectory;
        internal static readonly string UserSettingsFile;

        private static readonly string vsixPath;
        private static readonly string NoSettingsFile;

        private static bool keepDirtyVsInstance = keepDirtyInstanceInit;

        private static VsExperimentalInstance visualStudioInstance;
        private static Application application;

        static VS()
        {
            string testDll = Assembly.GetExecutingAssembly().Location;
            Match match = Regex.Match(testDll, @"^(.*)\\GoogleTestExtension\\GoogleTestAdapterUiTests\\bin\\(Debug|Release)\\GoogleTestAdapterUiTests.dll$");
            Assert.IsTrue(match.Success);

            string basePath = match.Groups[1].Value;
            string debugOrRelease = match.Groups[2].Value;
            vsixPath = Path.Combine(basePath, @"GoogleTestExtension\GoogleTestAdapterVSIX\bin", debugOrRelease, @"GoogleTestAdapterVSIX.vsix");
            SolutionDirectory = Path.Combine(basePath, @"SampleGoogleTestTests\SampleGoogleTestTests.sln");
            UiTestsDirectory = Path.Combine(basePath, @"GoogleTestExtension\GoogleTestAdapterUiTests");
            UserSettingsFile = Path.Combine(basePath, @"SampleGoogleTestTests\NonDeterministic.runsettings");
            NoSettingsFile = Path.Combine(basePath, @"SampleGoogleTestTests\No.runsettings");
        }

        internal static void SetupVanillaVsExperimentalInstance()
        {
            string solutionDir = Path.GetDirectoryName(SolutionDirectory);
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
                LogExceptionAndThrow(exception);
            }

            application = visualStudioInstance.Launch();
            CoreAppXmlConfiguration.Instance.ApplyTemporarySetting(
                c => { c.BusyTimeout = c.FindWindowTimeout = TimeOut; });
            MainWindow = application.GetWindow(
                SearchCriteria.ByAutomationId("VisualStudioMainWindow"),
                InitializeOption.NoCache);
        }

        internal static void CleanVsExperimentalInstance()
        {
            MainWindow.Dispose();
            application.Dispose();

            MainWindow = null;
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
            MainWindow.VsMenuBarMenuItems("File", "Open", "Project/Solution...").Click();
            FillFileDialog("Open Project", SolutionDirectory);
        }

        internal static void CloseSolution()
        {
            MainWindow.VsMenuBarMenuItems("File", "Close Solution").Click();
        }


        internal static void SelectTestSettingsFile(string settingsFile)
        {
            MainWindow.VsMenuBarMenuItems("Test", "Test Settings", "Select Test Settings File").Click();
            FillFileDialog("Open Settings File", settingsFile);
        }

        internal static void UnselectTestSettingsFile()
        {
            SelectTestSettingsFile(NoSettingsFile);
        }

        internal static IUIItem OpenTestExplorer()
        {
            MainWindow.VsMenuBarMenuItems("Test", "Windows", "Test Explorer").Click();
            return MainWindow.Get<UIItem>("TestWindowToolWindowControl");
        }

        private static void FillFileDialog(string dialogTitle, string file)
        {
            Window fileOpenDialog = MainWindow.ModalWindow(dialogTitle);
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

        private static void LogExceptionAndThrow(AutomationException exception, [CallerMemberName] string testCaseName = null)
        {
            string debugDetailsFile = Path.Combine(UiTestsDirectory, "TestErrors", typeof(UiTests).Name + "__" + testCaseName + "__DebugDetails.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(debugDetailsFile));
            File.WriteAllText(debugDetailsFile, exception.ToString() + "\r\n" + exception.StackTrace + "\r\n" + exception.DebugDetails);
            throw exception;
        }

    }

}