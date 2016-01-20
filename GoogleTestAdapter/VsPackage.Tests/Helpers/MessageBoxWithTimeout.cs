using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GoogleTestAdapterUiTests.Helpers
{
    // Thank you, Esge: http://stackoverflow.com/a/20098381/859211
    public static class MessageBoxWithTimeout
    {
        public static MessageBoxResult Show(string text, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxResult defaultResult = MessageBoxResult.OK, TimeSpan? timeoutOrDefault = null)
        {
            TimeSpan timeout = timeoutOrDefault ?? TimeSpan.FromSeconds(10);

            Window owner = new Window()
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Background = Brushes.Transparent,
                AllowsTransparency = true,
                ShowInTaskbar = false,
                ShowActivated = true,
                Topmost = true
            };
            owner.Show();

            IntPtr handle = new WindowInteropHelper(owner).Handle;
            Task.Delay((int)timeout.TotalMilliseconds).ContinueWith(
                t => SendMessage(handle, 0x10 /*WM_CLOSE*/, IntPtr.Zero, IntPtr.Zero));

            return MessageBox.Show(owner, text, caption, buttons, MessageBoxImage.None, defaultResult);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}
