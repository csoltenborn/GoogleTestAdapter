using System;
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

        public ReleaseNotesDialog()
        {
            InitializeComponent();

            WebBrowser.CanGoBackChanged += (sender, args) => BackButton.Enabled = WebBrowser.CanGoBack;
            BackButton.Click += (sender, args) => WebBrowser.GoBack();

            WebBrowser.CanGoForwardChanged += (sender, args) => ForwardButton.Enabled = WebBrowser.CanGoForward;
            ForwardButton.Click += (sender, args) => WebBrowser.GoForward();

            ShowReleaseNotesCheckBox.Checked = true;
            ShowReleaseNotesCheckBox.CheckedChanged += 
                (sender, args) => ShowReleaseNotesChanged?.Invoke(this, new ShowReleaseNotesChangedEventArgs { ShowReleaseNotes = ShowReleaseNotesCheckBox.Checked });

            OkButton.Click += (sender, args) => Close();
        }

        internal Uri HtmlFile
        {
            get { return WebBrowser.Url; }
            set { WebBrowser.Url = value; }
        }

    }

}