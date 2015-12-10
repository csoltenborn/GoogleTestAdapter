using System;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using TestStack.White.UIItems;
using TestStack.White.UIItems.ListBoxItems;
using TestStack.White.UIItems.WindowItems;

namespace GoogleTestAdapterUiTests.Helpers
{
    static class TestStackWhiteExtensions
    {
        // White's Window.MenuBar.MenuItem("Some", "Menu", "Option").Click() is broken for Visual Studio.
        // There is a simple fix, but we are bound to the broken version from nuget.
        // This workaround runs the command by entering "Some Menu Option" in the quick launch bar.
        // Use it like this:  Window.VsMenuBarMenuItems("Some", "Menu", "Option").Click()
        public static VsMenuBarWorkaround VsMenuBarMenuItems(this Window VsWindow, params string[] path)
        {
            return new VsMenuBarWorkaround(VsWindow, path);
        }

        // Print a UIItem and its children to Visual Studio's output window (like UIItem.LogStructure).
        public static void PrintStructure(this UIItem item)
        {
            System.Diagnostics.Debug.Print(TestStack.White.Debug.Details(item.AutomationElement));
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
    }

    public class VsMenuBarWorkaround
    {
        private Window VsWindow;
        private string[] path;

        public VsMenuBarWorkaround(Window VsWindow, string[] path)
        {
            this.VsWindow = VsWindow;
            this.path = path;
        }

        public void Click()
        {
            string menuSearch = string.Join(" ", path);
            string menuRegex = string.Join(".*", path);
            VsWindow.Get<TextBox>("PART_SearchBox").BulkText = menuSearch;
            Predicate<ListItem> menuItem = (item) => Regex.IsMatch(item.Name, menuRegex);
            VsWindow.Get<ListBox>("ResultsViewListBox").Items.Find(menuItem).Click();
        }
    }
}
