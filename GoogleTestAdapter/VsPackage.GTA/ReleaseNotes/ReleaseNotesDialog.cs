using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public partial class ReleaseNotesDialog : Form
    {
        public ReleaseNotesDialog(ReleaseNotesTheme theme)
        {
            InitializeComponent();
            ApplyTheme(theme);

            Load += (sender, args) => OkButton.Select();
            OkButton.Click += (sender, args) => Close();
        }

        internal Uri HtmlFile
        {
            get => WebBrowser.Url;
            set => WebBrowser.Url = value;
        }

        private void ApplyTheme(ReleaseNotesTheme theme)
        {
            _isDarkTheme = theme.Background.GetBrightness() < 0.5;
            BackColor = theme.Background;
            ForeColor = theme.Text;
            RootPanel.BackColor = theme.Background;
            OkButtonPanel.BackColor = theme.Background;

            OkButton.FlatStyle = FlatStyle.Flat;
            OkButton.BackColor = theme.ButtonBackground;
            OkButton.ForeColor = theme.ButtonText;
            OkButton.FlatAppearance.BorderColor = theme.ButtonBorder;
            OkButton.UseVisualStyleBackColor = false;
        }

        private bool _isDarkTheme;

        // the title bar follows Windows' theme unless set explicitly (Windows 10 1809 and later; ignored otherwise)
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int useDarkMode = _isDarkTheme ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
        }

        private static class NativeMethods
        {
            internal const int DwmwaUseImmersiveDarkMode = 20;

            [DllImport("dwmapi.dll")]
            internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        }

        // links are opened in the default browser rather than in the dialog's Internet Explorer control
        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.Scheme == Uri.UriSchemeHttp || e.Url.Scheme == Uri.UriSchemeHttps)
            {
                e.Cancel = true;
                Process.Start(e.Url.ToString());
            }
        }
    }

}
