// This file has been modified by Microsoft on 9/2017.

using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public partial class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        [LocalizedCategory("CategoryMiscName")]
        [LocalizedDisplayName("OptionShowReleaseNotes")]
        [LocalizedDescription("OptionShowReleaseNotesDescription")]
        public bool ShowReleaseNotes
        {
            get { return _showReleaseNotes; }
            set { SetAndNotify(ref _showReleaseNotes, value); }
        }
        private bool _showReleaseNotes = SettingsWrapper.OptionShowReleaseNotesDefaultValue;
    }

}