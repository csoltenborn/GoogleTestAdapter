using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public partial class ReleaseNotesDialog : Form
    {
        internal class ShowReleaseNotesChangedEventArgs : EventArgs
        {
            internal bool ShowReleaseNotes { get; set; }
        }

        internal event EventHandler<ShowReleaseNotesChangedEventArgs> ShowReleaseNotesChanged;

        private readonly ISet<Uri> _externalUris = new HashSet<Uri>();

        public ReleaseNotesDialog()
        {
            InitializeComponent();

            WebBrowser.CanGoBackChanged += (sender, args) => BackButton.Enabled = WebBrowser.CanGoBack;
            BackButton.Click += (sender, args) => WebBrowser.GoBack();

            WebBrowser.CanGoForwardChanged += (sender, args) => ForwardButton.Enabled = WebBrowser.CanGoForward;
            ForwardButton.Click += (sender, args) => WebBrowser.GoForward();

            ShowReleaseNotesCheckBox.CheckedChanged += 
                (sender, args) => ShowReleaseNotesChanged?.Invoke(this, new ShowReleaseNotesChangedEventArgs { ShowReleaseNotes = ShowReleaseNotesCheckBox.Checked });

            OkButton.Click += (sender, args) => Close();
        }

        internal Uri HtmlFile
        {
            get => WebBrowser.Url;
            set => WebBrowser.Url = value;
        }

        internal bool ShowReleaseNotesChecked
        {
            get => ShowReleaseNotesCheckBox.Checked;
            set => ShowReleaseNotesCheckBox.Checked = value;
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