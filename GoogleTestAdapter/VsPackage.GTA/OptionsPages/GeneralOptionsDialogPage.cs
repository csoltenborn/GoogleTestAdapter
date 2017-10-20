// This file has been modified by Microsoft on 6/2017.

using GoogleTestAdapter.Settings;
using System.ComponentModel;

namespace GoogleTestAdapter.VsPackage.OptionsPages
{

    public partial class GeneralOptionsDialogPage : NotifyingDialogPage
    {
        [Category(SettingsWrapper.CategoryMiscName)]
        [DisplayName(SettingsWrapper.OptionShowReleaseNotes)]
        [Description(SettingsWrapper.OptionShowReleaseNotesDescription)]
        public bool ShowReleaseNotes
        {
            get { return _showReleaseNotes; }
            set { SetAndNotify(ref _showReleaseNotes, value); }
        }
        private bool _showReleaseNotes = SettingsWrapper.OptionShowReleaseNotesDefaultValue;
    }

}