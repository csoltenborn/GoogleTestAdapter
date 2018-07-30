using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace GoogleTestAdapter.VsPackage.GTA.Shared.ReleaseNotes
{
    public partial class ReleaseNotesDialog : Form
    {
        public class ShowReleaseNotesChangedEventArgs : EventArgs
        {
            public bool ShowReleaseNotes { get; set; }
        }

        public event EventHandler<ShowReleaseNotesChangedEventArgs> ShowReleaseNotesChanged;

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

        public Uri HtmlFile
        {
            get => WebBrowser.Url;
            set => WebBrowser.Url = value;
        }

        public bool ShowReleaseNotesChecked
        {
            get => ShowReleaseNotesCheckBox.Checked;
            set => ShowReleaseNotesCheckBox.Checked = value;
        }

        public void AddExternalUri(Uri externalUri)
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