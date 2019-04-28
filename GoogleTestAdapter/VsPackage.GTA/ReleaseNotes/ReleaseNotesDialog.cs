using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public partial class ReleaseNotesDialog : Form
    {
        private static readonly Uri DonationsUri = new Uri("https://github.com/csoltenborn/GoogleTestAdapter#donations");

        public ReleaseNotesDialog()
        {
            InitializeComponent();

            Load += (sender, args) => OkButton.Select();

            WebBrowser.CanGoBackChanged += (sender, args) => BackButton.Enabled = WebBrowser.CanGoBack;
            BackButton.Click += (sender, args) => WebBrowser.GoBack();

            WebBrowser.CanGoForwardChanged += (sender, args) => ForwardButton.Enabled = WebBrowser.CanGoForward;
            ForwardButton.Click += (sender, args) => WebBrowser.GoForward();

            OkButton.Click += (sender, args) => Close();
            DonateButton.Click += (sender, args) =>
            {
                OpenUriInDefaultBrowser(DonationsUri);
                Close();
            };
        }

        internal Uri HtmlFile
        {
            get => WebBrowser.Url;
            set => WebBrowser.Url = value;
        }

        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (DonationsUri.Equals(e.Url))
            {
                e.Cancel = true;
                OpenUriInDefaultBrowser(e.Url);
            }
        }

        private void OpenUriInDefaultBrowser(Uri uri)
        {
            Process.Start(uri.ToString());
        }
    }

}