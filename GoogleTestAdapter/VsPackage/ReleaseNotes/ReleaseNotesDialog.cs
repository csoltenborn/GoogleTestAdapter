using System;
using System.Windows.Forms;

namespace GoogleTestAdapter.VsPackage.ReleaseNotes
{
    public partial class ReleaseNotesDialog : Form
    {
        internal event EventHandler ShowReleaseNotesChanged;

        public ReleaseNotesDialog(bool showReleaseNotes = true)
        {
            InitializeComponent();

            WebBrowser.CanGoBackChanged += (sender, args) => BackButton.Enabled = WebBrowser.CanGoBack;
            BackButton.Click += (sender, args) => WebBrowser.GoBack();

            WebBrowser.CanGoForwardChanged += (sender, args) => ForwardButton.Enabled = WebBrowser.CanGoForward;
            ForwardButton.Click += (sender, args) => WebBrowser.GoForward();

            ShowReleaseNotesCheckBox.Checked = showReleaseNotes;
            ShowReleaseNotesCheckBox.CheckedChanged += (sender, args) => ShowReleaseNotesChanged?.Invoke(this, args);

            OkButton.Click += (sender, args) => Close();
        }

        internal Uri HtmlFile
        {
            get { return WebBrowser.Url; }
            set { WebBrowser.Url = value; }
        }

        internal bool ShowReleaseNotes => ShowReleaseNotesCheckBox.Checked;

    }

}