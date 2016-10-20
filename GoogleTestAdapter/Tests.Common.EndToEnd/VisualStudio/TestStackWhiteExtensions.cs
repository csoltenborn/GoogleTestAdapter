using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using TestStack.White;
using TestStack.White.UIItems;
using TestStack.White.UIItems.ListBoxItems;
using TestStack.White.UIItems.WindowItems;

namespace GoogleTestAdapter.Tests.Common.EndToEnd.VisualStudio
{
    public static class TestStackWhiteExtensions
    {
        // White's Window.MenuBar.MenuItem("Some", "Menu", "Option").Click() is broken for Visual Studio.
        // There is a simple fix, but we are bound to the broken version from NuGet.
        // This workaround runs the command by entering "Some Menu Option" in the quick launch bar.
        // Use it like this:  Window.VsMenuBarMenuItems("Some", "Menu", "Option").Click()
        public static VsMenuBarWorkaround VsMenuBarMenuItems(this Window vsWindow, params string[] path)
        {
            return new VsMenuBarWorkaround(vsWindow, path);
        }

        // Print a UIItem and its children to Visual Studio's output window (like UIItem.LogStructure).
        public static void PrintStructure(this UIItem item)
        {
            System.Diagnostics.Debug.Print(Debug.Details(item.AutomationElement));
        }

        // Print an AutomationElement and its children to Visual Studio's output window.
        public static void PrintStructure(this AutomationElement element, int indent = 0)
        {
            string strIndent = new string(' ', indent * 3);
            System.Diagnostics.Debug.Print(strIndent + element.Current.ControlType.LocalizedControlType);
            System.Diagnostics.Debug.Print(strIndent + element.Current.ControlType.ProgrammaticName);
            System.Diagnostics.Debug.Print(strIndent + element.Current.Name);
            System.Diagnostics.Debug.Print(strIndent + element.Current.AutomationId);

            foreach (AutomationElement child in element.FindAll(TreeScope.Children, Condition.TrueCondition))
                child.PrintStructure(indent + 1);
        }

        public static void LogAndThrow(this AutomationException exception, string typename, [CallerMemberName] string testCaseName = null)
        {
            string debugDetailsFile = Path.Combine(new Vs().UiTestsDirectory, "TestErrors", typename + "__" + testCaseName + "__DebugDetails.txt");
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(debugDetailsFile));
            File.WriteAllText(debugDetailsFile, exception + "\r\n" + exception.StackTrace + "\r\n" + exception.DebugDetails);
            throw exception;
        }

    }

    public class VsMenuBarWorkaround
    {
        private readonly Window _vsWindow;
        private readonly string[] _path;

        public VsMenuBarWorkaround(Window vsWindow, string[] path)
        {
            _vsWindow = vsWindow;
            _path = path;
        }

        public void Click()
        {
            string menuSearch = string.Join(" ", _path);
            string menuRegex = string.Join(".*", _path);
            _vsWindow.Get<TextBox>("PART_SearchBox").BulkText = menuSearch;
            Predicate<ListItem> menuItem = (item) => Regex.IsMatch(item.Name, menuRegex);
            _vsWindow.Get<ListBox>("ResultsViewListBox").Items.Find(menuItem).Click();
        }
    }
}
