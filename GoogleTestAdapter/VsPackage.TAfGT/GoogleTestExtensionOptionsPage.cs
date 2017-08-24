// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace GoogleTestAdapter.VsPackage
{
    public partial class GoogleTestExtensionOptionsPage
    {
        private const string PackageGuidString = "1db31773-234b-424b-a887-b451fb1ba837";
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
