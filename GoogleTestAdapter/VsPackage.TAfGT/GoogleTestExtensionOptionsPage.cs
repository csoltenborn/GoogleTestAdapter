// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GoogleTestAdapter.VsPackage
{
    public partial class GoogleTestExtensionOptionsPage
    {
        private const string OptionsCategoryName = "Test Adapter for Google Test";

        private void DisplayReleaseNotesIfNecessary()
        {
            // TAfGT does not display release notes.
        }

        private bool ShowReleaseNotes
        {
            get { return false; }
        }
    }
}
