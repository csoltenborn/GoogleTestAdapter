using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using GoogleTestAdapter.VsPackage.GTA.ReleaseNotes;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public partial class ReleaseNotesDialog : Form
    {
        private readonly ISet<Uri> _externalUris = new HashSet<Uri>();

        public ReleaseNotesDialog()
        {
            InitializeComponent();

            Load += (sender, args) => DonateButton.Select();

            WebBrowser.CanGoBackChanged += (sender, args) => BackButton.Enabled = WebBrowser.CanGoBack;
            BackButton.Click += (sender, args) => WebBrowser.GoBack();

            WebBrowser.CanGoForwardChanged += (sender, args) => ForwardButton.Enabled = WebBrowser.CanGoForward;
            ForwardButton.Click += (sender, args) => WebBrowser.GoForward();

            OkButton.Click += (sender, args) => Close();
            DonateButton.Click += (sender, args) =>
            {
                OpenUriInDefaultBrowser(Donations.Uri);
                Close();
            };
        }

        internal Uri HtmlFile
        {
            get => WebBrowser.Url;
            set => WebBrowser.Url = value;
        }

        internal void AddExternalUri(Uri externalUri)
        {
            _externalUris.Add(externalUri);
        }

        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (_externalUris.Contains(e.Url))
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